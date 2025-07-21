using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;

namespace KeepersCompound.Lighting.Vis;

public static class VisGraphBuilder
{
    public static VisGraph FromCells(WorldRep.Cell[] cells)
    {
        var graph = new VisGraph();
        foreach (var cell in cells)
        {
            var edges = new List<VisGraphEdge>(cell.PortalPolyCount);

            // If a cell is "blocks vision" flagged, we can never see out of it
            // We can see into it though, so we still want the edges coming in
            if ((cell.Flags & 8) != 0)
            {
                graph.AddNode(edges);
                continue;
            }

            // We have to cycle through *all* polys rather than just portals to calculate the correct poly vertex offsets
            var indicesOffset = 0;
            var portalStartIdx = cell.PolyCount - cell.PortalPolyCount;
            for (var j = 0; j < cell.PolyCount; j++)
            {
                var cellPoly = cell.Polys[j];
                if (j < portalStartIdx)
                {
                    indicesOffset += cellPoly.VertexCount;
                    continue;
                }

                // Checking if there's already an edge is super slow. It's much faster to just add a new edge, even with
                // the duplicated poly
                var vs = new List<Vector3>(cellPoly.VertexCount);
                for (var vIdx = 0; vIdx < cellPoly.VertexCount; vIdx++)
                {
                    vs.Add(cell.Vertices[cell.Indices[indicesOffset + vIdx]]);
                }

                edges.Add(new VisGraphEdge(cellPoly.Destination, new VisGraphPoly(cell.Planes[cellPoly.PlaneId], vs)));
                indicesOffset += cellPoly.VertexCount;
            }

            graph.AddNode(edges);
        }

        return graph;
    }
}