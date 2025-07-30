using System.Numerics;

namespace KeepersCompound.Lighting.Vis;

public class VisGraph
{
    private const float Epsilon = MathUtils.Epsilon;

    private readonly List<List<VisGraphEdge>> _nodes = [];

    public void AddNode(List<VisGraphEdge> edges)
    {
        _nodes.Add(edges);
    }

    public HashSet<int> ComputeVisibleNodes(int startNode, Vector3 position, float maxRange)
    {
        if (startNode < 0 || startNode >= _nodes.Count)
        {
            return [];
        }

        var visibleNodes = new HashSet<int> { startNode };
        var visitedNodes = new Stack<int>();
        visitedNodes.Push(startNode);

        // Lists used in clipping. Pre-made here to avoid allocations each time we clip.
        var clipDistances = new List<float>(32);
        var clipSides = new List<VisGraphClipSide>(32);
        var clipCounts = new[] { 0, 0, 0 };

        foreach (var edge in _nodes[startNode])
        {
            ComputeVisibleNodesRecursive(
                visibleNodes,
                visitedNodes,
                position,
                maxRange,
                edge.Destination,
                edge.Poly,
                clipDistances,
                clipSides,
                clipCounts);
        }

        return visibleNodes;
    }

    private void ComputeVisibleNodesRecursive(
        HashSet<int> visibleNodes,
        Stack<int> visitedNodes,
        Vector3 position,
        float maxRange,
        int currentNode,
        VisGraphPoly passPoly,
        List<float> clipDistances,
        List<VisGraphClipSide> clipSides,
        int[] clipCounts)
    {
        visitedNodes.Push(currentNode);
        visibleNodes.Add(currentNode);

        var clipPlanes = new List<Plane>(passPoly.Vertices.Count);
        clipPlanes.Clear();
        for (var i = 0; i < passPoly.Vertices.Count; i++)
        {
            var v0 = passPoly.Vertices[i];
            var v1 = passPoly.Vertices[(i + 1) % passPoly.Vertices.Count];

            var normal = Vector3.Cross(v0 - position, v1 - position);
            if (normal.LengthSquared() < Epsilon)
            {
                continue;
            }

            normal = Vector3.Normalize(normal);
            var d = -Vector3.Dot(v1, normal);
            var plane = new Plane(normal, d);
            clipPlanes.Add(plane);
        }

        // This basically only happens if the pass poly is tiny
        if (clipPlanes.Count == 0)
        {
            visitedNodes.Pop();
            return;
        }

        foreach (var edge in _nodes[currentNode])
        {
            // This only checks is there is a point on the plane in range.
            // Could probably use poly center + radius to get an even better early out.
            if (visitedNodes.Contains(edge.Destination) ||
                (edge.Poly.Center - position).Length() > maxRange + edge.Poly.Radius ||
                MathUtils.DistanceFromNormalizedPlane(edge.Poly.Plane, position) < -Epsilon)
            {
                continue;
            }

            var poly = new VisGraphPoly(edge.Poly);
            foreach (var clipPlane in clipPlanes)
            {
                ClipPolygonByPlane(poly, clipPlane, clipDistances, clipSides, clipCounts);
            }

            if (poly.Vertices.Count == 0)
            {
                continue;
            }

            ComputeVisibleNodesRecursive(
                visibleNodes,
                visitedNodes,
                position,
                maxRange,
                edge.Destination,
                poly,
                clipDistances,
                clipSides,
                clipCounts);
        }

        visitedNodes.Pop();
    }

    private static void ClipPolygonByPlane(
        VisGraphPoly poly,
        Plane plane,
        List<float> clipDistances,
        List<VisGraphClipSide> clipSides,
        int[] clipCounts)
    {
        var vertexCount = poly.Vertices.Count;
        if (vertexCount == 0)
        {
            return;
        }

        // Firstly we want to tally up what side of the plane each point of the poly is on
        // This is used both to early out if nothing/everything is clipped, and to aid the clipping
        clipDistances.Clear();
        clipSides.Clear();
        clipCounts[0] = 0;
        clipCounts[1] = 0;
        clipCounts[2] = 0;
        for (var i = 0; i < vertexCount; i++)
        {
            var distance = MathUtils.DistanceFromPlane(plane, poly.Vertices[i]);
            var side = distance switch
            {
                > Epsilon => VisGraphClipSide.Front,
                < -Epsilon => VisGraphClipSide.Back,
                _ => VisGraphClipSide.On,
            };
            clipDistances.Add(distance);
            clipSides.Add(side);
            clipCounts[(int)side]++;
        }

        // Everything is within the half-space, so we don't need to clip anything
        if (clipCounts[(int)VisGraphClipSide.Back] == 0 && clipCounts[(int)VisGraphClipSide.On] != vertexCount)
        {
            return;
        }

        // Everything is outside the half-space, so we clip everything
        if (clipCounts[(int)VisGraphClipSide.Front] == 0)
        {
            poly.Vertices.Clear();
            return;
        }

        var vertices = new List<Vector3>();
        for (var i = 0; i < vertexCount; i++)
        {
            var i1 = (i + 1) % vertexCount;
            var v0 = poly.Vertices[i];
            var v1 = poly.Vertices[i1];
            var side = clipSides[i];
            var nextSide = clipSides[i1];

            // Vertices that are inside/on the half-space don't get clipped
            if (clipSides[i] != VisGraphClipSide.Back)
            {
                vertices.Add(v0);
            }

            // We only need to do any clipping if we've swapped from front-to-back or vice versa
            // If either the current or next side is On then that's where we would have clipped to
            // anyway so we also don't need to do anything
            if (side == VisGraphClipSide.On || nextSide == VisGraphClipSide.On || side == nextSide)
            {
                continue;
            }

            // This is how far along the vector v0 -> v1 the front/back crossover occurs
            var frac = clipDistances[i] / (clipDistances[i] - clipDistances[i1]);
            var splitVertex = v0 + frac * (v1 - v0);
            vertices.Add(splitVertex);
        }

        poly.Vertices.Clear();
        poly.Vertices.AddRange(vertices);
    }
}