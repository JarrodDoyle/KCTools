using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Maths;
using KeepersCompound.Portalisation.Brush;

namespace KeepersCompound.Portalisation;

public class CellBuilder
{
    public class Surface
    {
        public required int PlaneId;
        public required List<int> Indices;
        public required CsgMedia Medium;
        public required int Destination;
        public required BrushTexInfo TexInfo;
    }

    public bool NeedsSplit => Vertices.Count > 128 || Surfaces.Count > 64;
    public CsgMedia Medium { get; }
    public List<Vector3> Vertices { get; } = [];
    public List<Plane> Planes { get; } = [];
    public List<Surface> Surfaces { get; } = [];

    public CellBuilder(CsgMedia medium)
    {
        Medium = medium;
    }

    public void AddMergedPolys(List<TreeExtractionPoly> polys)
    {
        var mergedPolys = new List<TreeExtractionPoly>();
        foreach (var poly in polys)
        {
            MergeOrInsert(mergedPolys, poly);
        }

        foreach (var poly in mergedPolys)
        {
            Surfaces.Add(new Surface
            {
                PlaneId = AddMergedPlane(Planes, poly.Plane),
                Indices = poly.Winding.Vertices.Select(v => AddMergedVertex(Vertices, v)).ToList(),
                Medium = poly.RightNode?.Medium ?? CsgMedia.None,
                Destination = poly.RightNode?.CellId ?? -1,
                TexInfo = poly.TexInfo,
            });
        }
    }

    public void AddPoly(Plane plane, Winding winding, CsgMedia rightMedia, int destination, BrushTexInfo texInfo)
    {
        Surfaces.Add(new Surface
        {
            PlaneId = AddMergedPlane(Planes, plane),
            Indices = winding.Vertices.Select(v => AddMergedVertex(Vertices, v)).ToList(),
            Medium = rightMedia,
            Destination = destination,
            TexInfo = texInfo,
        });
    }

    public WorldRep.Cell ToCell()
    {
        var vertices = new List<Vector3>();
        var planes = new List<Plane>();
        var indices = new List<byte>();
        var polys = new List<WorldRep.Cell.Poly>();
        var renderPolys = new List<WorldRep.Cell.RenderPoly>();
        var lmInfos = new List<WorldRep.Cell.LightmapInfo>();
        var lms = new List<WorldRep.Cell.Lightmap>();
        var (processOrder, nonPortalVertices, portalPolyCount) = GetSurfaceProcessInfo();

        foreach (var surface in processOrder.Select(idx => Surfaces[idx]))
        {
            ProcessSurface(surface, vertices, planes, indices, polys, renderPolys, lmInfos, lms);
        }

        return new WorldRep.Cell(
            (byte)Medium,
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
            [..lmInfos],
            [..lms],
            [0]);
    }

    private (List<int>, int, int) GetSurfaceProcessInfo()
    {
        var processOrder = new List<int>(Surfaces.Count);
        var nonPortalVertices = 0;
        var renderPolyCount = 0;
        var portalPolyCount = 0;
        for (var i = 0; i < Surfaces.Count; i++)
        {
            var surface = Surfaces[i];
            if (surface.Medium == CsgMedia.Solid)
            {
                processOrder.Insert(i - portalPolyCount, i);
                renderPolyCount++;
                nonPortalVertices += surface.Indices.Count;
            }
            else if (Medium != surface.Medium)
            {
                processOrder.Insert(i - portalPolyCount, i);
                renderPolyCount++;
                portalPolyCount++;
            }
            else
            {
                processOrder.Insert(renderPolyCount, i);
                portalPolyCount++;
            }
        }

        return (processOrder, nonPortalVertices, portalPolyCount);
    }

    private void ProcessSurface(
        Surface surface,
        List<Vector3> vertices,
        List<Plane> planes,
        List<byte> indices,
        List<WorldRep.Cell.Poly> polys,
        List<WorldRep.Cell.RenderPoly> renderPolys,
        List<WorldRep.Cell.LightmapInfo> lmInfos,
        List<WorldRep.Cell.Lightmap> lms)
    {
        var vs = surface.Indices.Select(vIndex => Vertices[vIndex]).ToList();
        var lMed = (CsgMedia)((int)Medium % 3);
        var rMed = (CsgMedia)((int)surface.Medium % 3);
        var destination = surface.Destination;
        var (flags, texId, clutId) = lMed switch
        {
            CsgMedia.Air when rMed == CsgMedia.Water => (16, 247, 1),
            CsgMedia.Water when rMed == CsgMedia.Air => (16, 248, 2),
            _ => (0, 0, 0)
        };

        if (texId == 0)
        {
            texId = (int)surface.TexInfo.TextureId;
        }

        indices.AddRange(vs.Select(v => (byte)AddMergedVertex(vertices, v)));
        polys.Add(new WorldRep.Cell.Poly
        {
            VertexCount = (byte)surface.Indices.Count,
            PlaneId = (byte)AddMergedPlane(planes, Planes[surface.PlaneId]),
            Destination = (ushort)(destination == -1 ? 0 : destination),
            ClutId = (byte)clutId,
            Flags = (byte)flags,
        });

        if (lMed == rMed)
        {
            return;
        }

        var texInfo = surface.TexInfo;
        var baseU = (Vector3.Dot(texInfo.UProjection, vs[0]) / texInfo.UScale + texInfo.Offset.X) % 4;
        var baseV = (Vector3.Dot(texInfo.VProjection, vs[0]) / texInfo.VScale + texInfo.Offset.Y) % 4;

        var planeNorm = Planes[surface.PlaneId].Normal;
        var texU = texInfo.UScale * ProjectionLinearEquation(texInfo.UProjection, texInfo.VProjection, planeNorm);
        var texV = texInfo.VScale * ProjectionLinearEquation(texInfo.VProjection, texInfo.UProjection, planeNorm);

        var l1 = texU.Length();
        var l2 = texV.Length();
        var textureMagnitude = l1 > l2 ? l1 : l2;

        renderPolys.Add(new WorldRep.Cell.RenderPoly
        {
            TextureVectors = (texU, texV),
            TextureBases = (baseU, baseV),
            TextureMagnitude = textureMagnitude,
            Center = vs.Aggregate(Vector3.Zero, (c, v) => c + v) / vs.Count,
            TextureId = (ushort)texId,
            CachedSurface = 0
        });
        lms.Add(new WorldRep.Cell.Lightmap(8, 8, 1, 4));
        lmInfos.Add(new WorldRep.Cell.LightmapInfo
        {
            PaddedWidth = 8,
            Height = 8,
            Width = 8,
        });
    }

    private static void MergeOrInsert(List<TreeExtractionPoly> polys, TreeExtractionPoly newPoly)
    {
        for (var idx = 0; idx < polys.Count; idx++)
        {
            var poly = polys[idx];
            var lMed1 = (CsgMedia)((int)(poly.LeftNode?.Medium ?? CsgMedia.None) % 3);
            var rMed1 = (CsgMedia)((int)(poly.RightNode?.Medium ?? CsgMedia.None) % 3);
            var lMed2 = (CsgMedia)((int)(newPoly.LeftNode?.Medium ?? CsgMedia.None) % 3);
            var rMed2 = (CsgMedia)((int)(newPoly.RightNode?.Medium ?? CsgMedia.None) % 3);
            var dest1 = poly.RightNode?.CellId ?? -1;
            var dest2 = newPoly.RightNode?.CellId ?? -1;
            if (lMed1 != lMed2 || rMed1 != rMed2 || (rMed1 != CsgMedia.Solid && dest1 != dest2))
            {
                continue;
            }

            if (!poly.Plane.EqualsEpsilon(newPoly.Plane)) continue;
            if (poly.TexInfo != newPoly.TexInfo) continue;
            if (!poly.Winding.TryMerge(newPoly.Winding, poly.Plane.Normal, out var newWinding)) continue;

            poly.Winding.Vertices = newWinding.Vertices;
            polys.RemoveAt(idx);
            newPoly = poly;
            idx = -1; // It's about to get incremented to 0 by the loop, which is the value we really want
        }

        polys.Add(newPoly);
    }

    private static int AddMergedPlane(List<Plane> planes, Plane plane)
    {
        for (var i = 0; i < planes.Count; i++)
        {
            if (plane.EqualsEpsilon(planes[i]))
            {
                return i;
            }
        }

        planes.Add(plane);
        return planes.Count - 1;
    }

    private static int AddMergedVertex(List<Vector3> vertices, Vector3 vertex)
    {
        for (var i = 0; i < vertices.Count; i++)
        {
            if (vertex.EqualsEpsilon(vertices[i]))
            {
                return i;
            }
        }

        vertices.Add(vertex);
        return vertices.Count - 1;
    }

    /// <summary>
    /// Special cased linear equation solver for u = 1, v = n = 0
    /// </summary>
    /// <param name="u">U axis</param>
    /// <param name="v">V axis</param>
    /// <param name="n">Normal</param>
    /// <returns>Equation solution</returns>
    private static Vector3 ProjectionLinearEquation(Vector3 u, Vector3 v, Vector3 n)
    {
        var det = u.X * (v.Y * n.Z - n.Y * v.Z) +
                  v.X * (n.Y * u.Z - u.Y * n.Z) +
                  n.X * (u.Y * v.Z - v.Y * u.Z);

        return new Vector3(
            (v.Y * n.Z - n.Y * v.Z) / det,
            (v.Z * n.X - n.Z * v.X) / det,
            (v.X * n.Y - n.X * v.Y) / det
        );
    }
}