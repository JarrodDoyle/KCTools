using System.IO.Compression;
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
        var wrCells = ConstructWrCells(bspTree, bspPlanes, wrTreeNodes);
        var wrTree = new WorldRep.BspTree
        {
            PlaneCount = (uint)bspPlanes.Count,
            NodeCount = (uint)wrTreeNodes.Count,
            Planes = bspPlanes.ToArray(),
            Nodes = wrTreeNodes.ToArray()
        };
        var wr = ConstructWr(wrCells.Count, wrCells, wrTree);
        return (wr, bspTree);
    }

    private List<WorldRep.Cell> ConstructWrCells(BspNode tree, List<Plane> bspPlanes,
        List<WorldRep.BspTree.Node> wrTreeNodes)
    {
        var protoCells = new List<CellBuilder>();
        tree.Traverse(node =>
        {
            if (node.CellId != -1 && node.Polys.Count != 0)
            {
                protoCells.Add(new CellBuilder(node));
            }
        });

        // Splits cells until there's nothing left that's too complex
        var addedSplits = new List<(Plane, int, int)>(); // TODO Need more info for where to insert
        var splitOccurred = true;
        while (splitOccurred)
        {
            splitOccurred = false;
            var cellCount = protoCells.Count;
            for (var i = 0; i < cellCount; i++)
            {
                var cell = protoCells[i];
                if (!cell.NeedsSplit)
                {
                    continue;
                }

                Log.Debug("Splitting complex cell {i}. Vertices {VN}, Polys {PN}", i, cell.Vertices.Count,
                    cell.Surfaces.Count);
                splitOccurred = true;

                // Shitty splitting plane :)
                var aabb = new Aabb();
                aabb.AddPoints(cell.Vertices);
                var dims = aabb.Max - aabb.Min;

                Plane splitPlane;
                if (dims.X > dims.Y && dims.X > dims.Z)
                {
                    splitPlane = new Plane(Vector3.UnitX, -(aabb.Min.X + dims.X / 2f));
                }
                else if (dims.Y > dims.X && dims.Y > dims.Z)
                {
                    splitPlane = new Plane(Vector3.UnitY, -(aabb.Min.Y + dims.Y / 2f));
                }
                else
                {
                    splitPlane = new Plane(Vector3.UnitZ, -(aabb.Min.Z + dims.Z / 2f));
                }

                var leftCell = new CellBuilder(cell.Medium);
                var rightCell = new CellBuilder(cell.Medium);
                foreach (var surface in cell.Surfaces)
                {
                    var winding = new Winding();
                    foreach (var idx in surface.Indices)
                    {
                        winding.Vertices.Add(cell.Vertices[idx]);
                    }

                    var plane = cell.Planes[surface.PlaneId];
                    var (leftWinding, rightWinding) = winding.Split(splitPlane);
                    if (leftWinding.Vertices.Count > 0)
                    {
                        leftCell.AddPoly(plane, leftWinding, surface.LeftMedia, surface.RightMedia,
                            surface.Destination);
                    }

                    if (rightWinding.Vertices.Count > 0)
                    {
                        rightCell.AddPoly(plane, rightWinding, surface.LeftMedia, surface.RightMedia,
                            surface.Destination);
                    }

                    if (rightWinding.Vertices.Count == 0 || surface.Destination == -1)
                    {
                        continue;
                    }

                    // Split the appropriate surface on destination
                    // TODO:
                    //  - Get the surface with destination of US and remove it
                    //  - Construct a winding for it
                    //  - Split the winding
                    //  - Construct left and right surfaces and insert
                    var destCell = protoCells[surface.Destination];
                    for (var j = 0; j < destCell.Surfaces.Count; j++)
                    {
                        var destSurface = destCell.Surfaces[j];
                        if (destSurface.Destination != i)
                        {
                            continue;
                        }

                        destCell.Surfaces.RemoveAt(j);
                        var destWinding = new Winding();
                        foreach (var idx in destSurface.Indices)
                        {
                            destWinding.Vertices.Add(destCell.Vertices[idx]);
                        }

                        var destPlane = destCell.Planes[destSurface.PlaneId];
                        var (destWindingLeft, destWindingRight) = destWinding.Split(splitPlane);
                        destCell.AddPoly(destPlane, destWindingLeft, destSurface.LeftMedia, destSurface.RightMedia, i);
                        destCell.AddPoly(destPlane, destWindingRight, destSurface.LeftMedia, destSurface.RightMedia,
                            protoCells.Count); // new cell id
                        break;
                    }
                }

                // Border poly :)
                if (leftCell.Surfaces.Count > 0 && rightCell.Surfaces.Count > 0)
                {
                    var borderWinding = new Winding(splitPlane, _worldSize);
                    foreach (var plane in cell.Planes)
                    {
                        borderWinding.Clip(plane);
                    }

                    leftCell.AddPoly(splitPlane, borderWinding, cell.Medium, cell.Medium, protoCells.Count);
                    rightCell.AddPoly(splitPlane.Inverse(), borderWinding, cell.Medium, cell.Medium, i);
                }

                addedSplits.Add((splitPlane, i, protoCells.Count));
                protoCells[i] = leftCell;
                protoCells.Add(rightCell);
            }
        }

        foreach (var (plane, from, to) in addedSplits)
        {
            // foreach (var node in wrTreeNodes)
            var nodeCount = wrTreeNodes.Count;
            for (var i = 0; i < nodeCount; i++)
            {
                var node = wrTreeNodes[i];
                var flags = (node.ParentIndex >> 24) & 0xFF;
                if ((flags & 0x01) == 0 || node.InsideIndex != from)
                {
                    continue;
                }

                // TODO: How to check if needs reverse flag?
                node.ParentIndex ^= 0x01 << 24;
                node.PlaneId = bspPlanes.Count;
                node.InsideIndex = nodeCount;
                node.OutsideIndex = nodeCount + 1;
                bspPlanes.Add(plane);
                wrTreeNodes.Add(new WorldRep.BspTree.Node
                {
                    ParentIndex = i | 0x01 << 24,
                    CellId = -1,
                    PlaneId = -1,
                    InsideIndex = from,
                    OutsideIndex = 0,
                });
                wrTreeNodes.Add(new WorldRep.BspTree.Node
                {
                    ParentIndex = i | 0x01 << 24,
                    CellId = -1,
                    PlaneId = -1,
                    InsideIndex = to,
                    OutsideIndex = 0,
                });
                wrTreeNodes[i] = node;
                break;
            }
        }

        var cells = protoCells.Select(protoCell => protoCell.ToCell()).ToList();
        Log.Debug("Generated cell count : {C}", cells.Count);
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
            else if (node.Parent != null && node.Parent.CellId != -1)
            {
                node.CellId = node.Parent.CellId;
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