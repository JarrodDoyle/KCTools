using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using KeepersCompound.Dark.Database;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Maths;
using KeepersCompound.Portalisation.Brush;
using Serilog;
using Version = KeepersCompound.Dark.Database.Version;

namespace KeepersCompound.Portalisation;

public class Portaliser
{
    private readonly float _worldSize;

    public Portaliser(float worldSize)
    {
        _worldSize = worldSize;
    }

    public WorldRep Portalise(List<BrushDef> brushDefs, bool optimize = true)
    {
        var (planeManager, bspBrushes) = BuildBspBrushes(brushDefs);
        var bspTree = new BspNode(null);
        InsertBrushes(bspTree, planeManager, bspBrushes);
        var rawLeafs = ComputeBspMediums(bspTree, bspBrushes);
        var extractionPolys = BuildExtractionPolys(bspTree, planeManager);
        AssignTexInfo(bspBrushes, extractionPolys, planeManager);
        if (optimize)
        {
            var groupedPolys = BuildOptimizePolys(extractionPolys);
            MergeOptimizePolys(groupedPolys, planeManager);
            bspTree = BuildOptimizeBspTree(planeManager, groupedPolys);
            InsertBrushes(bspTree, planeManager, bspBrushes);
            var optimizeRawLeafs = ComputeBspMediums(bspTree, bspBrushes);
            extractionPolys = BuildExtractionPolys(bspTree, planeManager);
            AssignTexInfo(bspBrushes, extractionPolys, planeManager);
            Log.Information("Unique planes: {C}", groupedPolys.Count);
            Log.Information("Optimize leaf count: {l}", optimizeRawLeafs);
        }

        var cellBuilders = AssignCells(bspTree);
        var initialCellCount = cellBuilders.Count;
        ApplyExtractionPolys(planeManager, cellBuilders, extractionPolys);
        var wrTreeBuilder = new WrTreeBuilder();
        wrTreeBuilder.AddCsgTree(planeManager, bspTree);
        SplitComplexCells(planeManager, cellBuilders, wrTreeBuilder);

        var wrCells = cellBuilders.Select(protoCell => protoCell.ToCell(planeManager)).ToList();
        var wrTree = wrTreeBuilder.ToWrTree();
        var wr = ConstructWr(wrCells.Count, wrCells, wrTree);

        Log.Information("Inserted {N} brushes.", bspBrushes.Count);
        Log.Information("Leaf count: {l}", rawLeafs);
        Log.Information("Initial cell count: {CellCount}", initialCellCount);
        Log.Information("Final cell count: {CellCount}", wrCells.Count);
        return wr;
    }

    private (PlaneManager, List<InsertionBrush>) BuildBspBrushes(List<BrushDef> brushDefs)
    {
        var planeManager = new PlaneManager();
        var bspBrushes = new List<InsertionBrush>();
        for (var i = 0; i < brushDefs.Count; i++)
        {
            var brush = brushDefs[i];
            var polys = new List<TreeInsertionPoly>();

            // Builds the initial poly info for each face of the brush. Note that we're not clipping here because we've
            // not yet registered all the face planes
            foreach (var face in brush.Faces)
            {
                var planeId = planeManager.AddPlane(face.Plane);
                var baseWinding = new Winding(planeManager.GetPlane(planeId), _worldSize);
                polys.Add(new TreeInsertionPoly(false, planeId, baseWinding, face.TexInfo));
            }

            // Now that the planes for each brush face are registered, we can clip
            for (var j = 0; j < polys.Count; j++)
            {
                var poly = polys[j];
                for (var k = 0; k < polys.Count; k++)
                {
                    if (k == j) continue;
                    poly.Winding.Clip(planeManager.GetPlane(polys[k].Plane));
                }
            }

            // TODO: Verify that the plane merging didn't cause 2 brush planes to be merged?
            bspBrushes.Add(new InsertionBrush(i, brush.Operation, polys));
        }

        return (planeManager, bspBrushes);
    }

    private void InsertBrushes(BspNode root, PlaneManager planeManager, List<InsertionBrush> bspBrushes)
    {
        var stack = new Stack<(BspNode, List<InsertionBrush>)>();
        stack.Push((root, bspBrushes));
        while (stack.TryPop(out var info))
        {
            var (node, brushes) = info;

            if (node.LeftChild == null || node.RightChild == null)
            {
                // If we didn't find a split face that means every face of every remaining brush is already being used as a
                // split somewhere in the tree and the remaining brushes in out list are fully contained by the current node.
                if (!TryFindSplitFace(brushes, out var splitFace))
                {
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
            }

            var (leftBrushes, rightBrushes) = SplitBrushList(brushes, planeManager, node.SplitPlane);
            stack.Push((node.LeftChild, leftBrushes));
            stack.Push((node.RightChild, rightBrushes));
        }
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

    private (List<InsertionBrush>, List<InsertionBrush>) SplitBrushList(
        List<InsertionBrush> brushes,
        PlaneManager planeManager,
        int splitPlaneId)
    {
        var splitPlane = planeManager.GetPlane(splitPlaneId);
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
                var (leftBrush, rightBrush) = SplitBrush(brush, planeManager, splitPlaneId);
                leftBrushes.Add(leftBrush);
                rightBrushes.Add(rightBrush);
            }
        }

        return (leftBrushes, rightBrushes);
    }

    private (InsertionBrush, InsertionBrush) SplitBrush(
        InsertionBrush brush,
        PlaneManager planeManager,
        int splitPlaneId)
    {
        var splitPlane = planeManager.GetPlane(splitPlaneId);
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
                borderWinding.Clip(planeManager.GetPlane(face.Plane));
            }

            leftFaces.Add(new TreeInsertionPoly(true, splitPlaneId, borderWinding, new BrushTexInfo()));
            rightFaces.Add(new TreeInsertionPoly(true, -splitPlaneId, borderWinding.Reversed(),
                new BrushTexInfo()));
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

    private void SplitComplexCells(PlaneManager planeManager, List<CellBuilder> cells, WrTreeBuilder wrTreeBuilder)
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
                var dimElements = new List<float> { dims[0], dims[1], dims[2] };
                var splitPlane = dimElements.IndexOf(dimElements.Max()) switch
                {
                    0 => new Plane(Vector3.UnitX, -(aabb.Min.X + dims.X / 2f)),
                    1 => new Plane(Vector3.UnitY, -(aabb.Min.Y + dims.Y / 2f)),
                    _ => new Plane(Vector3.UnitZ, -(aabb.Min.Z + dims.Z / 2f)),
                };
                var splitPlaneId = planeManager.AddPlane(splitPlane);

                var leftCell = new CellBuilder(cell.Medium);
                var rightCell = new CellBuilder(cell.Medium);
                foreach (var surface in cell.Surfaces)
                {
                    var winding = new Winding();
                    foreach (var idx in surface.Indices)
                    {
                        winding.Vertices.Add(cell.Vertices[idx]);
                    }

                    var plane = cell.PlaneIds[surface.PlaneId];
                    var (leftWinding, rightWinding) = winding.Split(splitPlane);
                    if (leftWinding.Vertices.Count > 0)
                    {
                        leftCell.AddPoly(plane, leftWinding, surface.Medium, surface.Destination, surface.TexInfo);
                    }

                    if (rightWinding.Vertices.Count > 0)
                    {
                        rightCell.AddPoly(plane, rightWinding, surface.Medium, surface.Destination, surface.TexInfo);
                    }

                    if (rightWinding.Vertices.Count == 0 || surface.Destination == -1)
                    {
                        continue;
                    }

                    // TODO: This can be better handled.
                    //       If LeftWinding is empty then we can just flat reassign the poly destination rather than
                    //       trying to split it.

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

                        var destPlane = destCell.PlaneIds[destSurface.PlaneId];
                        var (destWindingLeft, destWindingRight) = destWinding.Split(splitPlane);
                        if (destWindingLeft.Vertices.Count > 0)
                        {
                            destCell.AddPoly(destPlane, destWindingLeft, destSurface.Medium, i, destSurface.TexInfo);
                        }

                        if (destWindingRight.Vertices.Count > 0)
                        {
                            destCell.AddPoly(destPlane, destWindingRight, destSurface.Medium, cells.Count,
                                destSurface.TexInfo); // new cell id
                        }

                        break;
                    }
                }

                // Border poly :)
                if (leftCell.Surfaces.Count > 0 && rightCell.Surfaces.Count > 0)
                {
                    var borderWinding = new Winding(splitPlane, _worldSize);
                    foreach (var plane in cell.PlaneIds)
                    {
                        borderWinding.Clip(planeManager.GetPlane(plane));
                    }

                    leftCell.AddPoly(splitPlaneId, borderWinding, cell.Medium, cells.Count, new BrushTexInfo());
                    rightCell.AddPoly(-splitPlaneId, borderWinding.Reversed(), cell.Medium, i,
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

    private List<TreeExtractionPoly> BuildExtractionPolys(BspNode node, PlaneManager planeManager)
    {
        if (node.LeftChild == null || node.RightChild == null)
        {
            return [];
        }

        var boundaryWinding = new Winding(planeManager.GetPlane(node.SplitPlane), _worldSize);
        var curNode = node;
        var parent = curNode.Parent;
        while (parent != null)
        {
            var splitPlaneId = parent.LeftChild == curNode ? parent.SplitPlane : -parent.SplitPlane;
            boundaryWinding.Clip(planeManager.GetPlane(splitPlaneId));
            curNode = parent;
            parent = parent.Parent;
        }

        var resultPolys = new List<TreeExtractionPoly>();
        resultPolys.AddRange(BuildExtractionPolys(node.LeftChild, planeManager));
        resultPolys.AddRange(BuildExtractionPolys(node.RightChild, planeManager));

        if (boundaryWinding.Vertices.Count < 3)
        {
            return resultPolys;
        }

        resultPolys.AddRange(ClipTreePolys(planeManager, node.RightChild, ClipTreePolys(planeManager, node.LeftChild, [
            new TreeExtractionPoly(node.SplitPlane, boundaryWinding, new BrushTexInfo(), node.LeftChild,
                node.RightChild)
        ])));
        return resultPolys;
    }

    private (List<TreeExtractionPoly>, List<TreeExtractionPoly>) SplitPolys(
        PlaneManager planeManager,
        BspNode node,
        List<TreeExtractionPoly> polys)
    {
        var leftPolys = new List<TreeExtractionPoly>();
        var rightPolys = new List<TreeExtractionPoly>();
        foreach (var poly in polys)
        {
            var nodeIsLeftSide = poly.LeftNode == node;
            var (left, right) = poly.Winding.Split(planeManager.GetPlane(node.SplitPlane));
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

    private List<TreeExtractionPoly> ClipTreePolys(
        PlaneManager planeManager,
        BspNode node,
        List<TreeExtractionPoly> polys)
    {
        if (polys.Count == 0 || node.Leaf)
        {
            return polys;
        }

        var (leftPolys, rightPolys) = SplitPolys(planeManager, node, polys);
        var result = ClipTreePolys(planeManager, node.LeftChild!, leftPolys);
        result.AddRange(ClipTreePolys(planeManager, node.RightChild!, rightPolys));
        return result;
    }

    private static void AssignTexInfo(
        List<InsertionBrush> brushes,
        List<TreeExtractionPoly> extractionPolys,
        PlaneManager planeManager)
    {
        var brushAabbs = new List<Aabb>();
        foreach (var brush in brushes)
        {
            var aabb = new Aabb();
            foreach (var face in brush.Faces)
            {
                aabb.AddPoints(face.Winding.Vertices);
            }

            brushAabbs.Add(aabb);
        }

        foreach (var extractionPoly in extractionPolys)
        {
            // We only care about poly's that will actually be rendered
            if (extractionPoly is { LeftNode: not null, RightNode: not null } &&
                extractionPoly.LeftNode.Medium == extractionPoly.RightNode.Medium)
            {
                continue;
            }

            var polyAabb = new Aabb();
            polyAabb.AddPoints(extractionPoly.Winding.Vertices);

            var bestTexInfo = new BrushTexInfo();
            var found = false;
            for (var i = 0; i < brushes.Count; i++)
            {
                var brushAabb = brushAabbs[i];
                if (!brushAabb.Contains(polyAabb)) continue;

                var coplanarFace = ContainedBrushFace(extractionPoly, brushes[i], planeManager);
                if (coplanarFace >= 0)
                {
                    bestTexInfo = brushes[i].Faces[coplanarFace].TexInfo;
                    found = true;
                }
            }

            if (!found)
            {
                Log.Error("Untextured poly: {P}", extractionPoly.Plane);
            }

            extractionPoly.TexInfo = bestTexInfo;
        }
    }

    /// <summary>
    /// Determine which face (if any) of a brush a poly lies on
    /// </summary>
    /// <param name="poly">The poly to check</param>
    /// <param name="brush">The brush it may lie on</param>
    /// <param name="planeManager">Plane manager to get brush plane details from</param>
    /// <returns>The face index if the brush contains the poly. Otherwise -1.</returns>
    private static int ContainedBrushFace(TreeExtractionPoly poly, InsertionBrush brush, PlaneManager planeManager)
    {
        var coplanarFace = -1;
        for (var i = 0; i < brush.Faces.Count; i++)
        {
            var (_, _, counts) = poly.Winding.GetSideDetails(planeManager.GetPlane(brush.Faces[i].Plane));
            if (counts[(int)Side.On] == poly.Winding.Vertices.Count) coplanarFace = i;
            else if (counts[(int)Side.Back] > 0) return -1;
        }

        return coplanarFace;
    }

    private static void ApplyExtractionPolys(
        PlaneManager planeManager,
        List<CellBuilder> cellBuilders,
        List<TreeExtractionPoly> extractionPolys)
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
            cellBuilders[i].AddMergedPolys(planeManager, polyLists[i]);
        }
    }

    private static List<(int, List<OptimizePoly>)> BuildOptimizePolys(List<TreeExtractionPoly> extractionPolys)
    {
        var groupedPolys = new List<(int, List<OptimizePoly>)>();
        foreach (var poly in extractionPolys)
        {
            if (poly is { LeftNode: not null, RightNode: not null } &&
                poly.LeftNode.Medium == poly.RightNode.Medium)
            {
                continue;
            }

            var added = false;
            foreach (var (plane, polys) in groupedPolys)
            {
                var sameDir = plane == poly.Plane;
                if (sameDir || plane == -poly.Plane)
                {
                    polys.Add(new OptimizePoly(poly.Winding, !sameDir));
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                groupedPolys.Add((poly.Plane, [new OptimizePoly(poly.Winding, false)]));
            }
        }

        return groupedPolys;
    }

    private static void MergeOptimizePolys(List<(int, List<OptimizePoly>)> groupedPolys, PlaneManager planeManager)
    {
        // TODO: Is this whole step necessary? I think it fundamentally doesn't really change the result.
        //       Really I need to benchmark if it's faster to do this + BSP, or just BSP raw.
        foreach (var (planeId, polys) in groupedPolys)
        {
            var plane = planeManager.GetPlane(planeId);
            var mergedPolys = new List<OptimizePoly>();
            foreach (var originalPoly in polys)
            {
                var newPoly = originalPoly;
                for (var i = 0; i < mergedPolys.Count; i++)
                {
                    var poly = mergedPolys[i];
                    if (newPoly.Flipped != poly.Flipped) continue;

                    var normal = newPoly.Flipped ? -plane.Normal : plane.Normal;
                    if (!poly.Winding.TryMerge(newPoly.Winding, normal, out var newWinding)) continue;

                    poly.Winding.Vertices = newWinding.Vertices;
                    mergedPolys.RemoveAt(i);
                    newPoly = poly;
                    i = -1;
                }

                mergedPolys.Add(newPoly);
            }

            polys.Clear();
            polys.AddRange(mergedPolys);
        }
    }

    private static BspNode BuildOptimizeBspTree(
        PlaneManager planeManager,
        List<(int, List<OptimizePoly>)> groupedOptimizePolys)
    {
        // TODO: Choose better plane
        var root = new BspNode(null);
        var stack = new Stack<(BspNode, List<(int, List<OptimizePoly>)>)>();
        stack.Push((root, groupedOptimizePolys));
        while (stack.TryPop(out var info))
        {
            var (node, groupedPolys) = info;
            if (groupedPolys.Count == 0) continue;

            var splitIndex = -1;
            var bestScore = int.MaxValue;
            for (var i = 0; i < groupedPolys.Count; i++)
            {
                var splitPlaneId = groupedPolys[i].Item1;
                var score = ScoreOptimizeSplitPlane(planeManager, splitPlaneId, groupedPolys);
                if (score < bestScore)
                {
                    splitIndex = i;
                    bestScore = score;
                }
            }

            var planeId = groupedPolys[splitIndex].Item1;
            groupedPolys.RemoveAt(splitIndex);

            node.SplitPlane = planeId;
            node.LeftChild = new BspNode(node);
            node.RightChild = new BspNode(node);

            var (leftGroupedPolys, rightGroupedPolys) = SplitGroupedPolys(planeManager, groupedPolys, planeId);
            stack.Push((node.LeftChild, leftGroupedPolys));
            stack.Push((node.RightChild, rightGroupedPolys));
        }

        return root;
    }

    private static int ScoreOptimizeSplitPlane(
        PlaneManager planeManager,
        int splitPlaneId,
        List<(int, List<OptimizePoly>)> groupedPolys)
    {
        var splitPlane = planeManager.GetPlane(splitPlaneId);
        var splits = 0;
        var front = 0;
        var back = 0;
        foreach (var (planeId, polys) in groupedPolys)
        {
            foreach (var poly in polys)
            {
                if (planeId == splitPlaneId) continue;

                var (_, _, counts) = poly.Winding.GetSideDetails(splitPlane);
                if (counts[(int)Side.Back] == 0)
                {
                    front++;
                }
                else if (counts[(int)Side.Front] == 0)
                {
                    back++;
                }
                else
                {
                    splits++;
                }
            }
        }

        var absNorm = Vector3.Abs(splitPlane.Normal);
        var axial = absNorm.EqualsEpsilon(Vector3.UnitX) ||
                    absNorm.EqualsEpsilon(Vector3.UnitY) ||
                    absNorm.EqualsEpsilon(Vector3.UnitZ);
        return splits * 2 + int.Abs(front - back) + (axial ? 0 : (front + back + splits) / 10);
    }

    private static (List<(int, List<OptimizePoly>)>, List<(int, List<OptimizePoly>)>) SplitGroupedPolys(
            PlaneManager planeManager,
            List<(int, List<OptimizePoly>)> groupedPolys,
            int splitPlaneId)
    {
        var splitPlane = planeManager.GetPlane(splitPlaneId);
        var leftGroupedPolys = new List<(int, List<OptimizePoly>)>();
        var rightGroupedPolys = new List<(int, List<OptimizePoly>)>();
        foreach (var group in groupedPolys)
        {
            // TODO: The majority of planes are axial, so there's probably an early out check for parallel planes
            var (plane, polys) = group;
            var fullCounts = new[] { 0, 0, 0 };
            foreach (var poly in polys)
            {
                var (_, _, counts) = poly.Winding.GetSideDetails(splitPlane);
                fullCounts[0] += counts[0];
                fullCounts[1] += counts[1];
                fullCounts[2] += counts[2];
            }

            if (fullCounts[(int)Side.Back] == 0)
            {
                leftGroupedPolys.Add(group);
            }
            else if (fullCounts[(int)Side.Front] == 0)
            {
                rightGroupedPolys.Add(group);
            }
            else
            {
                var leftPolys = new List<OptimizePoly>();
                var rightPolys = new List<OptimizePoly>();
                foreach (var poly in polys)
                {
                    var (leftWinding, rightWinding) = poly.Winding.Split(splitPlane);
                    if (leftWinding.Vertices.Count > 0) leftPolys.Add(new OptimizePoly(leftWinding, poly.Flipped));
                    if (rightWinding.Vertices.Count > 0) rightPolys.Add(new OptimizePoly(rightWinding, poly.Flipped));
                }

                leftGroupedPolys.Add((plane, leftPolys));
                rightGroupedPolys.Add((plane, rightPolys));
            }
        }

        return (leftGroupedPolys, rightGroupedPolys);
    }
}