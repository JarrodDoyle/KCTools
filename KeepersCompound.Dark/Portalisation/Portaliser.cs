using System.Diagnostics.CodeAnalysis;
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

    public (WorldRep, BspNode) Portalise(List<BrushDef> brushDefs)
    {
        var bspBrushes = BuildBspBrushes(brushDefs);
        var bspTree = BuildBspTree(bspBrushes);
        var rawLeafs = ComputeBspMediums(bspTree, bspBrushes);
        var cellBuilders = AssignCells(bspTree);
        var initialCellCount = cellBuilders.Count;
        var extractionPolys = BuildExtractionPolys(bspTree);
        ApplyExtractionPolys(cellBuilders, extractionPolys);
        var wrTreeBuilder = new WrTreeBuilder();
        wrTreeBuilder.AddCsgTree(bspTree);
        SplitComplexCells(cellBuilders, wrTreeBuilder);

        var wrCells = cellBuilders.Select(protoCell => protoCell.ToCell()).ToList();
        var wrTree = wrTreeBuilder.ToWrTree();
        var wr = ConstructWr(wrCells.Count, wrCells, wrTree);

        Log.Information("Inserted {N} brushes.", bspBrushes.Count);
        Log.Information("Leaf count: {l}", rawLeafs);
        Log.Information("Initial cell count: {CellCount}", initialCellCount);
        Log.Information("Final cell count: {CellCount}", wrCells.Count);
        return (wr, bspTree);
    }

    private List<InsertionBrush> BuildBspBrushes(List<BrushDef> brushDefs)
    {
        var bspBrushes = new List<InsertionBrush>();
        for (var i = 0; i < brushDefs.Count; i++)
        {
            var brush = brushDefs[i];
            var faces = new List<TreeInsertionPoly>();
            for (var j = 0; j < brush.Faces.Count; j++)
            {
                var winding = new Winding(brush.Faces[j].Plane, _worldSize);
                for (var k = 0; k < brush.Faces.Count; k++)
                {
                    if (k == j) continue;
                    winding.Clip(brush.Faces[k].Plane);
                }

                faces.Add(new TreeInsertionPoly(false, brush.Faces[j].Plane, winding, brush.Faces[j].TexInfo));
            }

            bspBrushes.Add(new InsertionBrush(i, brush.Operation, faces));
        }

        return bspBrushes;
    }

    private BspNode BuildBspTree(List<InsertionBrush> bspBrushes)
    {
        var root = new BspNode(null);
        var stack = new Stack<(BspNode, List<InsertionBrush>)>();
        stack.Push((root, bspBrushes));
        while (stack.TryPop(out var info))
        {
            var (node, brushes) = info;

            // If we didn't find a split face that means every face of every remaining brush is already being used as a
            // split somewhere in the tree and the remaining brushes in out list are fully contained by the current node.
            // This *should* mean that the current node is a leaf.
            if (!TryFindSplitFace(brushes, out var splitFace))
            {
                if (!node.Leaf)
                {
                    Log.Error("Couldn't find split for non-leaf node.");
                }

                foreach (var brush in brushes)
                {
                    node.InsertedBrushIds.Add(brush.Time);
                }

                continue;
            }

            splitFace.UsedForSplit = true;
            node.SplitPlane = splitFace.Plane;
            node.LeftChild = new BspNode(node);
            node.RightChild = new BspNode(node);
            node.TexInfo = splitFace.TexInfo;

            var (leftBrushes, rightBrushes) = SplitBrushList(brushes, splitFace.Plane);
            stack.Push((node.LeftChild, leftBrushes));
            stack.Push((node.RightChild, rightBrushes));
        }

        return root;
    }

    /// <summary>
    /// Attempts to select a face to use for splitting the brush list
    /// </summary>
    /// <param name="brushes">The brushes to choose a split face from. Also, the list of brushes that will be split by the face.</param>
    /// <param name="splitFace">The face to be used for splitting.</param>
    /// <returns>True if a valid splitter is found, false otherwise.</returns>
    private static bool TryFindSplitFace(
        List<InsertionBrush> brushes,
        [NotNullWhen(true)] out TreeInsertionPoly? splitFace)
    {
        // TODO: Actually evaluate if this is a good split rather than just picking the first one
        foreach (var brush in brushes)
        {
            foreach (var face in brush.Faces)
            {
                if (face.UsedForSplit) continue;

                splitFace = face;
                return true;
            }
        }

        splitFace = null;
        return false;
    }

    private (List<InsertionBrush>, List<InsertionBrush>) SplitBrushList(List<InsertionBrush> brushes, Plane splitPlane)
    {
        var leftBrushes = new List<InsertionBrush>();
        var rightBrushes = new List<InsertionBrush>();
        foreach (var brush in brushes)
        {
            var fullCounts = new[] { 0, 0, 0 };
            foreach (var face in brush.Faces)
            {
                var (_, _, counts) = face.Winding.GetSideDetails(splitPlane);
                fullCounts[0] += counts[0];
                fullCounts[1] += counts[1];
                fullCounts[2] += counts[2];

                if (counts[(int)Side.On] == face.Winding.Vertices.Count)
                {
                    face.UsedForSplit = true;
                }
            }

            if (fullCounts[(int)Side.Back] == 0)
            {
                leftBrushes.Add(brush);
            }
            else if (fullCounts[(int)Side.Front] == 0)
            {
                rightBrushes.Add(brush);
            }
            else
            {
                var (leftBrush, rightBrush) = SplitBrush(brush, splitPlane);
                leftBrushes.Add(leftBrush);
                rightBrushes.Add(rightBrush);
            }
        }

        return (leftBrushes, rightBrushes);
    }

    private (InsertionBrush, InsertionBrush) SplitBrush(InsertionBrush brush, Plane splitPlane)
    {
        var leftFaces = new List<TreeInsertionPoly>();
        var rightFaces = new List<TreeInsertionPoly>();
        foreach (var face in brush.Faces)
        {
            var (leftWinding, rightWinding) = face.Winding.Split(splitPlane);
            if (leftWinding.Vertices.Count > 0)
            {
                leftFaces.Add(new TreeInsertionPoly(face.UsedForSplit, face.Plane, leftWinding, face.TexInfo));
            }

            if (rightWinding.Vertices.Count > 0)
            {
                rightFaces.Add(new TreeInsertionPoly(face.UsedForSplit, face.Plane, rightWinding, face.TexInfo));
            }
        }

        if (leftFaces.Count > 0 && rightFaces.Count > 0)
        {
            var borderWinding = new Winding(splitPlane, _worldSize);
            foreach (var face in brush.Faces)
            {
                borderWinding.Clip(face.Plane);
            }

            leftFaces.Add(new TreeInsertionPoly(true, splitPlane, borderWinding, new BrushTexInfo()));
            rightFaces.Add(new TreeInsertionPoly(true, splitPlane.Inverse(), borderWinding, new BrushTexInfo()));
        }
        else
        {
            Log.Warning("Splitting brush resulted in an empty side.");
        }

        return (
            new InsertionBrush(brush.Time, brush.Operation, leftFaces),
            new InsertionBrush(brush.Time, brush.Operation, rightFaces));
    }

    /// <summary>
    /// Recursively computes the medium at each tree node, merging mediums when both children are the same.
    /// </summary>
    /// <param name="node">The current node being computed.</param>
    /// <param name="bspBrushes">List of all brushes that have been inserted into the tree.</param>
    /// <returns>The number of virtual leafs after computing and merging mediums</returns>
    private int ComputeBspMediums(BspNode node, List<InsertionBrush> bspBrushes)
    {
        var rawLeafs = 0;
        if (node.Leaf)
        {
            node.Medium = node.InsertedBrushIds.Aggregate(
                CsgMedia.Solid, (m, t) => CsgMediaTable.GetMedium(bspBrushes[t].Operation, m)
            );
        }
        else if (node is { LeftChild: not null, RightChild: not null })
        {
            rawLeafs += ComputeBspMediums(node.LeftChild, bspBrushes);
            rawLeafs += ComputeBspMediums(node.RightChild, bspBrushes);
            if (node.LeftChild.Medium == node.RightChild.Medium)
            {
                node.Medium = node.LeftChild.Medium;
            }
            else
            {
                if (node.LeftChild.Medium != CsgMedia.None) rawLeafs++;
                if (node.RightChild.Medium != CsgMedia.None) rawLeafs++;
            }
        }
        else
        {
            Log.Error("Non-leaf node with null child when computing BSP medium.");
        }

        return rawLeafs;
    }

    private void SplitComplexCells(List<CellBuilder> cells, WrTreeBuilder wrTreeBuilder)
    {
        var splitOccurred = true;
        while (splitOccurred)
        {
            splitOccurred = false;
            var cellCount = cells.Count;
            for (var i = 0; i < cellCount; i++)
            {
                var cell = cells[i];
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
                            surface.Destination, surface.TexInfo);
                    }

                    if (rightWinding.Vertices.Count > 0)
                    {
                        rightCell.AddPoly(plane, rightWinding, surface.LeftMedia, surface.RightMedia,
                            surface.Destination, surface.TexInfo);
                    }

                    if (rightWinding.Vertices.Count == 0 || surface.Destination == -1)
                    {
                        continue;
                    }

                    // Split the appropriate surface on destination
                    var destCell = cells[surface.Destination];
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
                        destCell.AddPoly(destPlane, destWindingLeft, destSurface.LeftMedia, destSurface.RightMedia, i,
                            destSurface.TexInfo);
                        destCell.AddPoly(destPlane, destWindingRight, destSurface.LeftMedia, destSurface.RightMedia,
                            cells.Count, destSurface.TexInfo); // new cell id
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

                    leftCell.AddPoly(splitPlane, borderWinding, cell.Medium, cell.Medium, cells.Count,
                        new BrushTexInfo());
                    rightCell.AddPoly(splitPlane.Inverse(), borderWinding, cell.Medium, cell.Medium, i,
                        new BrushTexInfo());
                }

                wrTreeBuilder.AddSplit(splitPlane, i, cells.Count);
                cells[i] = leftCell;
                cells.Add(rightCell);
            }
        }
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

    private List<CellBuilder> AssignCells(BspNode root)
    {
        var cellBuilders = new List<CellBuilder>();
        var stack = new Stack<BspNode>();
        stack.Push(root);
        while (stack.TryPop(out var node))
        {
            if (node is { LeftChild: not null, RightChild: not null })
            {
                stack.Push(node.LeftChild);
                stack.Push(node.RightChild);
            }

            if (node.Parent != null && node.Parent.Medium == node.Medium)
            {
                node.CellId = node.Parent.CellId;
            }
            else if (node.Medium != CsgMedia.None && node.Medium != CsgMedia.Solid)
            {
                node.CellId = cellBuilders.Count;
                cellBuilders.Add(new CellBuilder(node.Medium));
            }
        }

        return cellBuilders;
    }

    private List<TreeExtractionPoly> BuildExtractionPolys(BspNode node)
    {
        if (node.LeftChild == null || node.RightChild == null)
        {
            return [];
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

        var resultPolys = new List<TreeExtractionPoly>();
        resultPolys.AddRange(BuildExtractionPolys(node.LeftChild));
        resultPolys.AddRange(BuildExtractionPolys(node.RightChild));

        if (boundaryWinding.Vertices.Count < 3)
        {
            return resultPolys;
        }

        resultPolys.AddRange(ClipTreePolys(node.RightChild, ClipTreePolys(node.LeftChild, [
            new TreeExtractionPoly(node.SplitPlane, boundaryWinding, node.TexInfo, node.LeftChild, node.RightChild)
        ])));
        return resultPolys;
    }

    private (List<TreeExtractionPoly>, List<TreeExtractionPoly>) SplitPolys(BspNode node,
        List<TreeExtractionPoly> polys, float epsilon = 0.001f)
    {
        var leftPolys = new List<TreeExtractionPoly>();
        var rightPolys = new List<TreeExtractionPoly>();
        foreach (var poly in polys)
        {
            var nodeIsLeftSide = poly.LeftNode == node;
            var (left, right) = poly.Winding.Split(node.SplitPlane, epsilon);
            if (left.Vertices.Count > 0)
            {
                leftPolys.Add(nodeIsLeftSide
                    ? new TreeExtractionPoly(poly.Plane, left, poly.TexInfo, node.LeftChild, poly.RightNode)
                    : new TreeExtractionPoly(poly.Plane, left, poly.TexInfo, poly.LeftNode, node.LeftChild));
            }

            if (right.Vertices.Count > 0)
            {
                rightPolys.Add(nodeIsLeftSide
                    ? new TreeExtractionPoly(poly.Plane, right, poly.TexInfo, node.RightChild, poly.RightNode)
                    : new TreeExtractionPoly(poly.Plane, right, poly.TexInfo, poly.LeftNode, node.RightChild));
            }
        }

        return (leftPolys, rightPolys);
    }

    private List<TreeExtractionPoly> ClipTreePolys(BspNode node, List<TreeExtractionPoly> polys)
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

    private static void ApplyExtractionPolys(List<CellBuilder> cellBuilders, List<TreeExtractionPoly> extractionPolys)
    {
        var polyLists = new List<List<TreeExtractionPoly>>(cellBuilders.Count);
        for (var i = 0; i < cellBuilders.Count; i++)
        {
            polyLists.Add([]);
        }

        foreach (var poly in extractionPolys)
        {
            if (poly.LeftNode == null || poly.RightNode == null) continue;
            if (poly.LeftNode.CellId == poly.RightNode.CellId) continue;

            if (poly.LeftNode.CellId >= 0) polyLists[poly.LeftNode.CellId].Add(poly);
            if (poly.RightNode.CellId >= 0) polyLists[poly.RightNode.CellId].Add(poly.Reversed());
        }

        for (var i = 0; i < cellBuilders.Count; i++)
        {
            cellBuilders[i].AddMergedPolys(polyLists[i]);
        }
    }
}