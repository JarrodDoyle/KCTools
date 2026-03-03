using System.Numerics;
using KeepersCompound.Dark.Database;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Portalisation.Brush;
using KeepersCompound.Dark.Maths;
using Serilog;
using Version = KeepersCompound.Dark.Database.Version;

namespace KeepersCompound.Dark.Portalisation;

public class Portaliser
{
    private readonly float _worldSize;

    public Portaliser(float worldSize)
    {
        _worldSize = worldSize;
    }

    public (WorldRep, BspNode) Portalise(BrList brushList)
    {
        var brushDefs = BrushListBuilder.FromChunk(brushList);
        // TODO: - Insert blockable brushes

        var bspTree = new BspNode(null);
        foreach (var brush in brushDefs)
        {
            InsertBrush(bspTree, brush.BuildPolys(_worldSize), brush);
        }

        Log.Information("Inserted {N} brushes.", brushDefs.Count);
        Log.Information("Leaf count: {l}", bspTree.EncodeMedium());

        var borderPolys = WorldBorderPolys(bspTree);
        var treePolys = CreateTreePolys(bspTree, borderPolys);
        foreach (var poly in treePolys)
        {
            InsertBspGeo(poly);
        }

        var cellCount = AssignCellIds(bspTree);
        Log.Information("Assigned {CellCount} cell IDs", cellCount);

        var bspPlanes = new List<Plane>();
        var wrTreeNodes = new List<WorldRep.BspTree.Node>();
        ConstructWrTreeNodes(bspPlanes, bspTree, wrTreeNodes, 0x00FFFFFF, -1);
        var wrTree = new WorldRep.BspTree
        {
            PlaneCount = (uint)bspPlanes.Count,
            NodeCount = (uint)wrTreeNodes.Count,
            Planes = bspPlanes.ToArray(),
            Nodes = wrTreeNodes.ToArray()
        };

        var wrCells = ConstructWrCells(bspTree);
        var wr = ConstructWr(cellCount, wrCells, wrTree);
        return (wr, bspTree);
    }

    private List<WorldRep.Cell> ConstructWrCells(BspNode tree)
    {
        var cells = new List<WorldRep.Cell>();

        tree.Traverse(node =>
        {
            if (node.CellId == -1)
            {
                return;
            }

            // TODO: Merge same verts, planes, etc.
            var vertices = new List<Vector3>();
            var indices = new List<byte>();
            var planes = new List<Plane>();
            var polys = new List<WorldRep.Cell.Poly>();
            var renderPolys = new List<WorldRep.Cell.RenderPoly>();
            var lightmapInfos = new List<WorldRep.Cell.LightmapInfo>();
            var lightmaps = new List<WorldRep.Cell.Lightmap>();

            // TODO: SET THE FORMAT ON WR/USE FORMAT FROM RENDPARAMS
            var dummyLm = new WorldRep.Cell.Lightmap(8, 8, 1, 4);
            var dummyLmInfo = new WorldRep.Cell.LightmapInfo
            {
                PaddedWidth = 8,
                Height = 8,
                Width = 8,
            };

            var nonPortalVertices = 0;
            var polyProcessOrder = new List<int>();
            var renderPolyCount = 0;
            var portalPolyCount = 0;
            for (var i = 0; i < node.Polys.Count; i++)
            {
                var poly = node.Polys[i];
                if (poly.RightNode is { Medium: CsgMedia.Solid })
                {
                    polyProcessOrder.Insert(i - portalPolyCount, i);
                    renderPolyCount++;
                }
                else if (poly is { LeftNode: not null, RightNode: not null } &&
                         poly.LeftNode.Medium != poly.RightNode.Medium)
                {
                    polyProcessOrder.Insert(i - portalPolyCount, i);
                    renderPolyCount++;
                    portalPolyCount++;
                }
                else
                {
                    polyProcessOrder.Insert(renderPolyCount, i);
                    portalPolyCount++;
                }
            }

            for (var i = 0; i < polyProcessOrder.Count; i++)
            {
                var bspPoly = node.Polys[polyProcessOrder[i]];
                var center = Vector3.Zero;
                foreach (var vertex in bspPoly.Winding.Vertices)
                {
                    center += vertex;
                    var existingFound = false;
                    for (var j = 0; j < vertices.Count; j++)
                    {
                        if ((vertex - vertices[j]).LengthSquared() < 0.001f)
                        {
                            indices.Add((byte)j);
                            existingFound = true;
                            break;
                        }
                    }

                    if (!existingFound)
                    {
                        indices.Add((byte)vertices.Count);
                        vertices.Add(vertex);
                    }
                }

                // TODO: Set flag |= 4 when non-lightmapped surface
                var lMed = (CsgMedia)((int)bspPoly.LeftNode!.Medium % 3);
                var rMed = (CsgMedia)((int)bspPoly.RightNode!.Medium % 3);
                var (flags, texId, clutId) = lMed switch
                {
                    CsgMedia.Air when rMed == CsgMedia.Water => (16, 247, 1),
                    CsgMedia.Water when rMed == CsgMedia.Air => (16, 248, 2),
                    _ => (0, 0, 0)
                };

                if (i < renderPolyCount)
                {
                    renderPolys.Add(new WorldRep.Cell.RenderPoly
                    {
                        TextureId = (ushort)texId,
                        TextureVectors = (Vector3.UnitX, Vector3.UnitY),
                        TextureMagnitude = 4,
                        Center = center / bspPoly.Winding.Vertices.Count,
                    });

                    lightmapInfos.Add(dummyLmInfo);
                    lightmaps.Add(dummyLm);
                }

                var planeId = -1;
                for (var j = 0; j < planes.Count; j++)
                {
                    if (Vector3.Dot(bspPoly.Plane.Normal, planes[j].Normal) > 0.9999f &&
                        float.Abs(bspPoly.Plane.D - planes[j].D) <= 0.001f)
                    {
                        planeId = j;
                        break;
                    }
                }

                if (planeId == -1)
                {
                    planeId = planes.Count;
                    planes.Add(bspPoly.Plane);
                }

                var destination = bspPoly.RightNode!.CellId;
                polys.Add(new WorldRep.Cell.Poly
                {
                    VertexCount = (byte)bspPoly.Winding.Vertices.Count,
                    PlaneId = (byte)planeId,
                    Destination = (ushort)(destination == -1 ? 0 : destination),
                    ClutId = (byte)clutId,
                    Flags = (byte)flags,
                });

                if (destination == -1)
                {
                    nonPortalVertices += bspPoly.Winding.Vertices.Count;
                }
            }

            cells.Add(new WorldRep.Cell(
                (byte)node.Medium,
                0,
                0,
                [..vertices],
                [..indices],
                [..planes],
                [..polys],
                [..renderPolys],
                (byte)portalPolyCount,
                nonPortalVertices,
                (ushort)indices.Count,
                [],
                [..lightmapInfos],
                [..lightmaps],
                [0]));

            if (vertices.Count > 128)
            {
                Log.Debug("Too many cell vertices: {N}", vertices.Count);
            }

            if (polys.Count > 64)
            {
                Log.Debug("Too many cell polys: {N}", polys.Count);
            }
        });
        return cells;
    }

    private WorldRep ConstructWr(int cellCount, List<WorldRep.Cell> cells, WorldRep.BspTree tree)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(cellCount);
        foreach (var cell in cells)
        {
            writer.Write(new byte[cell.RenderPolyCount * 4]);
        }

        writer.Write(0);
        var bytes = stream.ToArray();

        var wr = new WorldRep
        {
            Header = new ChunkHeader { Name = "WREXT", Version = new Version { Major = 0, Minor = 30 } },
            DataHeader = new WorldRep.WrHeader
            {
                Size = 20,
                Version = 5,
                CellCount = (uint)cellCount,
                LightmapFormat = 1,
            },
            Cells = cells.ToArray(),
            Bsp = tree,
            CellZones = new WorldRep.CellZone[cellCount],
            LightingTable = new WorldRep.LightTable(),
            UnreadData = bytes,
        };

        for (var i = 0; i < cellCount; i++)
        {
            wr.CellZones[i] = new WorldRep.CellZone();
        }

        return wr;
    }

    private int AssignCellIds(BspNode tree)
    {
        var cellCount = 0;
        tree.Traverse(node =>
        {
            if (node.Polys.Count > 0)
            {
                node.CellId = cellCount++;
            }
        });

        return cellCount;
    }

// TODO: Use cell based planes rather than spaffing global plane indices
    private void ConstructWrTreeNodes(
        List<Plane> bspPlanes,
        BspNode tree,
        List<WorldRep.BspTree.Node> nodes,
        int parentIndex,
        int cellId)
    {
        // According to vfig (see vfig/misdeed) Marked flag (0x2) gets cleared on load so I can just ignore it
        var node = new WorldRep.BspTree.Node
        {
            ParentIndex = parentIndex,
            CellId = -1,
            PlaneId = -1,
            InsideIndex = 0x00FFFFFF,
            OutsideIndex = 0x00FFFFFF,
        };

        cellId = tree.CellId == -1 ? cellId : tree.CellId;
        if (tree.Leaf || tree.CellId != -1)
        {
            node.ParentIndex |= 0x01 << 24;
            node.InsideIndex = cellId;
            node.OutsideIndex = 0; // DromEd writes garbage here. It's just padding and means nothing
            nodes.Add(node);
            return;
        }

        node.PlaneId = bspPlanes.Count;
        bspPlanes.Add(tree.SplitPlane);
        nodes.Add(node);
        parentIndex = nodes.Count - 1;
        if (tree.LeftChild != null && tree.LeftChild.Medium != CsgMedia.Solid)
        {
            node.InsideIndex = nodes.Count;
            ConstructWrTreeNodes(bspPlanes, tree.LeftChild, nodes, parentIndex, cellId);
        }

        if (tree.RightChild != null && tree.RightChild.Medium != CsgMedia.Solid)
        {
            node.OutsideIndex = nodes.Count;
            ConstructWrTreeNodes(bspPlanes, tree.RightChild, nodes, parentIndex, cellId);
        }

        if (node.OutsideIndex != 0x00FFFFFF && node.InsideIndex == 0x00FFFFFF)
        {
            node.InsideIndex = node.OutsideIndex;
            node.OutsideIndex = 0x00FFFFFF;
            node.ParentIndex |= 0x4 << 24;
        }

        nodes[parentIndex] = node;
    }

    private void InsertBrush(BspNode node, List<BspPoly> polys, BrushDef brush)
    {
        if (polys.All(poly => poly.Coplanar))
        {
            node.ContainedBrushes.Add(brush);
            return;
        }

        if (node.Leaf)
        {
            var splitNode = node;
            foreach (var poly in polys.Where(poly => !poly.Coplanar))
            {
                splitNode.SplitPlane = poly.Plane;
                splitNode.LeftChild = new BspNode(splitNode);
                splitNode.RightChild = new BspNode(splitNode);
                splitNode = splitNode.LeftChild;
            }

            splitNode.ContainedBrushes.Add(brush);
            return;
        }

        var fullCounts = new[] { 0, 0, 0 };
        foreach (var poly in polys)
        {
            var (_, _, counts) = poly.Winding.GetSideDetails(node.SplitPlane);
            if (counts[(int)Side.On] == poly.Winding.Vertices.Count)
            {
                poly.Coplanar = true;
            }

            fullCounts[0] += counts[0];
            fullCounts[1] += counts[1];
            fullCounts[2] += counts[2];
        }

        if (fullCounts[(int)Side.Back] == 0)
        {
            InsertBrush(node.LeftChild!, polys, brush);
        }
        else if (fullCounts[(int)Side.Front] == 0)
        {
            InsertBrush(node.RightChild!, polys, brush);
        }
        else
        {
            var (left, right) = SplitPolys(node, polys);
            if (left.Count > 0 && right.Count > 0)
            {
                // TODO: Get world size here :)
                var borderWinding = new Winding(node.SplitPlane, _worldSize);
                foreach (var poly in polys)
                {
                    borderWinding.Clip(poly.Plane);
                }

                left.Add(new BspPoly(node.SplitPlane, borderWinding, coplanar: true));
                right.Add(new BspPoly(node.SplitPlane.Inverse(), borderWinding, coplanar: true));
            }

            InsertBrush(node.LeftChild!, left, brush);
            InsertBrush(node.RightChild!, right, brush);
        }
    }

    private List<BspPoly> CreateTreePolys(BspNode node, List<BspPoly> polys)
    {
        if (node.Leaf)
        {
            return polys;
        }

        var boundaryWinding = new Winding(node.SplitPlane, _worldSize);
        var curNode = node;
        var parent = curNode.Parent;
        while (parent != null)
        {
            boundaryWinding.Clip(parent.LeftChild == curNode ? parent.SplitPlane : parent.SplitPlane.Inverse());
            curNode = parent;
            parent = parent.Parent;
        }

        foreach (var poly in polys)
        {
            boundaryWinding.Clip(poly.LeftNode == node ? poly.Plane : poly.Plane.Inverse());
        }

        var (leftPolys, rightPolys) = SplitPolys(node, polys);
        var resultPolys = new List<BspPoly>();
        resultPolys.AddRange(CreateTreePolys(node.LeftChild!, leftPolys));
        resultPolys.AddRange(CreateTreePolys(node.RightChild!, rightPolys));

        if (boundaryWinding.Vertices.Count < 3)
        {
            return resultPolys;
        }

        var boundaryPoly = new BspPoly(node.SplitPlane, boundaryWinding, node.LeftChild, node.RightChild);
        resultPolys.AddRange(ClipTreePolys(node.RightChild!, ClipTreePolys(node.LeftChild!, [boundaryPoly])));
        return resultPolys;
    }

    private (List<BspPoly>, List<BspPoly>) SplitPolys(BspNode node, List<BspPoly> polys, float epsilon = 0.001f)
    {
        var leftPolys = new List<BspPoly>();
        var rightPolys = new List<BspPoly>();
        foreach (var poly in polys)
        {
            var nodeIsLeftSide = poly.LeftNode == node;
            var (left, right) = poly.Winding.Split(node.SplitPlane, epsilon);
            if (left.Vertices.Count > 0)
            {
                leftPolys.Add(nodeIsLeftSide
                    ? new BspPoly(poly.Plane, left, node.LeftChild, poly.RightNode, poly.Coplanar)
                    : new BspPoly(poly.Plane, left, poly.LeftNode, node.LeftChild, poly.Coplanar));
            }

            if (right.Vertices.Count > 0)
            {
                rightPolys.Add(nodeIsLeftSide
                    ? new BspPoly(poly.Plane, right, node.RightChild, poly.RightNode, poly.Coplanar)
                    : new BspPoly(poly.Plane, right, poly.LeftNode, node.RightChild, poly.Coplanar));
            }
        }

        return (leftPolys, rightPolys);
    }

    private List<BspPoly> ClipTreePolys(BspNode node, List<BspPoly> polys)
    {
        if (polys.Count == 0 || node.Leaf)
        {
            return polys;
        }

        var (leftPolys, rightPolys) = SplitPolys(node, polys);
        var result = ClipTreePolys(node.LeftChild!, leftPolys);
        result.AddRange(ClipTreePolys(node.RightChild!, rightPolys));
        return result;
    }

    private List<BspPoly> WorldBorderPolys(BspNode root)
    {
        return new Plane[]
        {
            new(-1, 0, 0, _worldSize), // North
            new(0, -1, 0, _worldSize), // West
            new(1, 0, 0, _worldSize), // South
            new(0, 1, 0, _worldSize), // East
            new(0, 0, 1, _worldSize), // Top
            new(0, 0, -1, _worldSize) // Bottom
        }.Select(p => new BspPoly(p, new Winding(p, _worldSize), root, root)).ToList();
    }

// BUG: This gives flipped faces sometimes?
    private void InsertBspGeo(BspPoly poly)
    {
        if (poly.LeftNode == null || poly.RightNode == null)
        {
            return;
        }

        var targetNodes = new List<BspNode?>(2);
        foreach (var node in new[] { poly.LeftNode, poly.RightNode })
        {
            var medium = node.Medium;
            if (medium is CsgMedia.Solid or CsgMedia.None)
            {
                targetNodes.Add(null);
                continue;
            }

            var targetNode = node;
            while (targetNode.Parent != null && targetNode.Parent.Medium == medium)
            {
                targetNode = targetNode.Parent;
            }

            targetNodes.Add(targetNode);
        }

        if (targetNodes[0] == targetNodes[1])
        {
            return;
        }

        targetNodes[0]?.Polys.Add(poly);
        targetNodes[1]?.Polys.Add(new BspPoly(
            poly.Plane.Inverse(),
            poly.Winding.Reversed(),
            poly.RightNode,
            poly.LeftNode,
            poly.Coplanar));
    }
}