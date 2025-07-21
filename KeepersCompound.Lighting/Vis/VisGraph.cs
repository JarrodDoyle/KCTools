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

        foreach (var edge in _nodes[startNode])
        {
            ComputeVisibleNodesRecursive(visibleNodes, visitedNodes, position, maxRange, edge.Destination, edge.Poly);
        }

        return visibleNodes;
    }

    private void ComputeVisibleNodesRecursive(
        HashSet<int> visibleNodes,
        Stack<int> visitedNodes,
        Vector3 position,
        float maxRange,
        int currentNode,
        VisGraphPoly passPoly)
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

        // foreach (var targetEdgeIdx in _graph[currentNode].EdgeIndices)
        foreach (var edge in _nodes[currentNode])
        {
            // This only checks is there is a point on the plane in range.
            // Could probably use poly center + radius to get an even better early out.
            if (visitedNodes.Contains(edge.Destination) ||
                Math.Abs(MathUtils.DistanceFromNormalizedPlane(edge.Poly.Plane, position)) > maxRange)
            {
                continue;
            }

            var poly = edge.Poly with { Vertices = [..edge.Poly.Vertices] };
            foreach (var clipPlane in clipPlanes)
            {
                ClipPolygonByPlane(ref poly, clipPlane);
            }

            if (poly.Vertices.Count == 0)
            {
                continue;
            }

            ComputeVisibleNodesRecursive(visibleNodes, visitedNodes, position, maxRange, edge.Destination, poly);
        }

        visitedNodes.Pop();
    }

    private static void ClipPolygonByPlane(ref VisGraphPoly poly, Plane plane)
    {
        var vertexCount = poly.Vertices.Count;
        if (vertexCount == 0)
        {
            return;
        }

        // Firstly we want to tally up what side of the plane each point of the poly is on
        // This is used both to early out if nothing/everything is clipped, and to aid the clipping
        var distances = new float[vertexCount];
        var sides = new VisGraphClipSide[vertexCount];
        var counts = new[] { 0, 0, 0 };
        for (var i = 0; i < vertexCount; i++)
        {
            var distance = MathUtils.DistanceFromPlane(plane, poly.Vertices[i]);
            distances[i] = distance;
            sides[i] = distance switch
            {
                > Epsilon => VisGraphClipSide.Front,
                < -Epsilon => VisGraphClipSide.Back,
                _ => VisGraphClipSide.On,
            };
            counts[(int)sides[i]]++;
        }

        // Everything is within the half-space, so we don't need to clip anything
        if (counts[(int)VisGraphClipSide.Back] == 0 && counts[(int)VisGraphClipSide.On] != vertexCount)
        {
            return;
        }

        // Everything is outside the half-space, so we clip everything
        if (counts[(int)VisGraphClipSide.Front] == 0)
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
            var side = sides[i];
            var nextSide = sides[i1];

            // Vertices that are inside/on the half-space don't get clipped
            if (sides[i] != VisGraphClipSide.Back)
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
            var frac = distances[i] / (distances[i] - distances[i1]);
            var splitVertex = v0 + frac * (v1 - v0);
            vertices.Add(splitVertex);
        }

        poly.Vertices.Clear();
        poly.Vertices.AddRange(vertices);
    }
}