using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Portalisation.Brush;
using KeepersCompound.Dark.Maths;
using Serilog;

namespace KeepersCompound.Dark.Portalisation;

public class Portaliser
{
    private readonly float _worldSize;

    public Portaliser(float worldSize)
    {
        _worldSize = worldSize;
    }

    public BspNode Portalise(WorldRep worldRep, BrList brushList)
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

        return bspTree;
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

    private void InsertBspGeo(BspPoly poly)
    {
        if (poly.LeftNode == null || poly.RightNode == null)
        {
            return;
        }

        foreach (var node in new[] { poly.LeftNode, poly.RightNode })
        {
            var medium = node.Medium;
            if (medium is CsgMedia.Solid or CsgMedia.None)
            {
                continue;
            }

            var targetNode = node;
            while (targetNode.Parent != null && targetNode.Parent.Medium == medium)
            {
                targetNode = targetNode.Parent;
            }

            if (node != poly.LeftNode)
            {
                poly.Plane = poly.Plane.Inverse();
                poly.Winding = poly.Winding.Reversed();
            }

            targetNode.Polys.Add(poly);
        }
    }
}