using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Maths;
using KeepersCompound.Dark.Portalisation.Brush;

namespace KeepersCompound.Dark.Portalisation;

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

        // This is wholy inaccurate, but just to make things vaguely identifiable on every face
        var planeNorm = Planes[surface.PlaneId].Normal;
        var p0 = vs[0];
        var p1 = vs[1];
        var xAxis = p1 - p0;
        var yAxis = Vector3.Cross(planeNorm, xAxis);
        xAxis = Vector3.Normalize(xAxis);
        yAxis = Vector3.Normalize(yAxis);

        renderPolys.Add(new WorldRep.Cell.RenderPoly
        {
            TextureVectors = (xAxis, yAxis),
            TextureMagnitude = 4,
            Center = vs.Aggregate(Vector3.Zero, (c, v) => c + v) / vs.Count,
            TextureId = (ushort)texId
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

            // TODO: Handle ensuring same texture data!
            if (!PlanesAreEqual(poly.Plane, newPoly.Plane))
            {
                continue;
            }

            var (i, j) = FindSharedEdge(poly.Winding.Vertices, newPoly.Winding.Vertices);
            if (i == -1 || j == -1)
            {
                continue;
            }

            var side1 = NextVertexSide(poly, newPoly, i, j);
            var side2 = NextVertexSide(newPoly, poly, j, i);
            if (side1 == Side.Front || side2 == Side.Front)
            {
                continue;
            }

            // Merge winding into poly!
            var vs1 = poly.Winding.Vertices;
            var vs2 = newPoly.Winding.Vertices;
            var newVertices = new List<Vector3>();
            for (var k = (i + (side2 == Side.On ? 2 : 1)) % vs1.Count; k != i; k = (k + 1) % vs1.Count)
            {
                newVertices.Add(vs1[k]);
            }

            for (var k = (j + (side1 == Side.On ? 2 : 1)) % vs2.Count; k != j; k = (k + 1) % vs2.Count)
            {
                newVertices.Add(vs2[k]);
            }

            poly.Winding.Vertices = newVertices;
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
            if (PlanesAreEqual(plane, planes[i]))
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
            if (VerticesAreEqual(vertex, vertices[i]))
            {
                return i;
            }
        }

        vertices.Add(vertex);
        return vertices.Count - 1;
    }

    private static (int, int) FindSharedEdge(List<Vector3> vs1, List<Vector3> vs2)
    {
        for (var i = 0; i < vs1.Count; i++)
        {
            var p1 = vs1[i];
            var p2 = vs1[(i + 1) % vs1.Count];
            for (var j = 0; j < vs2.Count; j++)
            {
                var p3 = vs2[j];
                var p4 = vs2[(j + 1) % vs2.Count];
                if (VerticesAreEqual(p1, p4) && VerticesAreEqual(p2, p3))
                {
                    return (i, j);
                }
            }
        }

        return (-1, -1);
    }

    private static Side NextVertexSide(TreeExtractionPoly p1, TreeExtractionPoly p2, int i, int j)
    {
        const float epsilon = 0.00001f;
        var v1 = p1.Winding.Vertices[i];
        var v2 = p1.Winding.Vertices[(i + p1.Winding.Vertices.Count - 1) % p1.Winding.Vertices.Count];
        var v3 = p2.Winding.Vertices[(j + 2) % p2.Winding.Vertices.Count];
        var dot = Vector3.Dot(v3 - v1, Vector3.Normalize(Vector3.Cross(p1.Plane.Normal, v1 - v2)));
        return dot < epsilon ? dot > -epsilon ? Side.On : Side.Back : Side.Front;
    }

    private static bool PlanesAreEqual(Plane p1, Plane p2)
    {
        const float epsilon = 0.00001f;
        return Vector3.Dot(p1.Normal, p2.Normal) > 1 - epsilon && float.Abs(p1.D - p2.D) <= epsilon;
    }

    private static bool VerticesAreEqual(Vector3 v1, Vector3 v2)
    {
        const float epsilon = 0.00001f;
        return (v1 - v2).LengthSquared() < epsilon;
    }
}