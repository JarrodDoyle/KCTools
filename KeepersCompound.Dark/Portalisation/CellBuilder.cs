using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Maths;
using Serilog;

namespace KeepersCompound.Dark.Portalisation;

public class CellBuilder
{
    private readonly List<BspPoly> _bspPolys = [];
    private readonly List<int> _polyProcessOrder = [];
    private int _nonPortalVertices;
    private int _renderPolyCount;
    private int _portalPolyCount;
    private readonly List<Vector3> _vertices = [];
    private readonly List<Plane> _planes = [];
    private readonly List<byte> _indices = [];
    private readonly List<WorldRep.Cell.Poly> _polys = [];
    private readonly List<WorldRep.Cell.RenderPoly> _renderPolys = [];
    private readonly List<WorldRep.Cell.LightmapInfo> _lightmapInfos = [];
    private readonly List<WorldRep.Cell.Lightmap> _lightmaps = [];

    public CellBuilder AddBspPolys(IEnumerable<BspPoly> polys)
    {
        var mergedPolys = MergeBspPolys(polys);
        foreach (var poly in mergedPolys)
        {
            var i = _bspPolys.Count;
            _bspPolys.Add(poly);
            if (poly.RightNode is { Medium: CsgMedia.Solid })
            {
                _polyProcessOrder.Insert(i - _portalPolyCount, i);
                _renderPolyCount++;
                _nonPortalVertices += poly.Winding.Vertices.Count;
            }
            else if (poly is { LeftNode: not null, RightNode: not null } &&
                     poly.LeftNode.Medium != poly.RightNode.Medium)
            {
                _polyProcessOrder.Insert(i - _portalPolyCount, i);
                _renderPolyCount++;
                _portalPolyCount++;
            }
            else
            {
                _polyProcessOrder.Insert(_renderPolyCount, i);
                _portalPolyCount++;
            }
        }

        return this;
    }

    public WorldRep.Cell Build(CsgMedia medium)
    {
        foreach (var index in _polyProcessOrder)
        {
            ProcessPoly(_bspPolys[index]);
        }

        return new WorldRep.Cell(
            (byte)medium,
            0,
            0,
            [.._vertices],
            [.._indices],
            [.._planes],
            [.._polys],
            [.._renderPolys],
            (byte)_portalPolyCount,
            _nonPortalVertices,
            (ushort)_indices.Count,
            [],
            [.._lightmapInfos],
            [.._lightmaps],
            [0]);
    }

    private void ProcessPoly(BspPoly bspPoly)
    {
        var vs = bspPoly.Winding.Vertices;
        var lMed = (CsgMedia)((int)(bspPoly.LeftNode?.Medium ?? CsgMedia.None) % 3);
        var rMed = (CsgMedia)((int)(bspPoly.RightNode?.Medium ?? CsgMedia.None) % 3);
        var destination = bspPoly.RightNode?.CellId ?? -1;
        var (flags, texId, clutId) = lMed switch
        {
            CsgMedia.Air when rMed == CsgMedia.Water => (16, 247, 1),
            CsgMedia.Water when rMed == CsgMedia.Air => (16, 248, 2),
            _ => (0, 0, 0)
        };

        _indices.AddRange(vs.Select(AddMergedVertex));
        _polys.Add(new WorldRep.Cell.Poly
        {
            VertexCount = (byte)bspPoly.Winding.Vertices.Count,
            PlaneId = (byte)AddMergedPlane(bspPoly.Plane),
            Destination = (ushort)(destination == -1 ? 0 : destination),
            ClutId = (byte)clutId,
            Flags = (byte)flags,
        });

        if (lMed != rMed)
        {
            _renderPolys.Add(new WorldRep.Cell.RenderPoly
            {
                TextureVectors = (Vector3.UnitX, Vector3.UnitY),
                TextureMagnitude = 4,
                Center = vs.Aggregate(Vector3.Zero, (c, v) => c + v) / vs.Count,
                TextureId = (ushort)texId
            });
            _lightmaps.Add(new WorldRep.Cell.Lightmap(8, 8, 1, 4));
            _lightmapInfos.Add(new WorldRep.Cell.LightmapInfo
            {
                PaddedWidth = 8,
                Height = 8,
                Width = 8,
            });
        }
    }

    private byte AddMergedVertex(Vector3 vertex)
    {
        const float epsilon = 0.001f;
        for (var i = 0; i < _vertices.Count; i++)
        {
            if ((vertex - _vertices[i]).LengthSquared() < epsilon)
            {
                return (byte)i;
            }
        }

        _vertices.Add(vertex);
        return (byte)(_vertices.Count - 1);
    }

    private int AddMergedPlane(Plane plane)
    {
        const float epsilon = 0.001f;
        for (var i = 0; i < _planes.Count; i++)
        {
            if (Vector3.Dot(plane.Normal, _planes[i].Normal) > 1 - epsilon &&
                float.Abs(plane.D - _planes[i].D) <= epsilon)
            {
                return i;
            }
        }

        _planes.Add(plane);
        return _planes.Count - 1;
    }

    private static List<BspPoly> MergeBspPolys(IEnumerable<BspPoly> polys)
    {
        var mergedPolys = new List<BspPoly>();
        var count = 0;
        foreach (var poly in polys)
        {
            count++;
            MergeOrInsert(mergedPolys, poly);
        }

        if (mergedPolys.Count != count)
            Log.Debug("PCount: {C1}, MPCount: {C2}", count, mergedPolys.Count);
        return mergedPolys;
    }

    private static void MergeOrInsert(List<BspPoly> polys, BspPoly newPoly)
    {
        foreach (var poly in polys)
        {
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
            return;
        }

        polys.Add(newPoly);
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

    private static Side NextVertexSide(BspPoly p1, BspPoly p2, int i, int j)
    {
        const float epsilon = 0.001f;
        var v1 = p1.Winding.Vertices[i];
        var v2 = p1.Winding.Vertices[(i + p1.Winding.Vertices.Count - 1) % p1.Winding.Vertices.Count];
        var v3 = p2.Winding.Vertices[(j + 2) % p2.Winding.Vertices.Count];
        var dot = Vector3.Dot(v3 - v1, Vector3.Normalize(Vector3.Cross(p1.Plane.Normal, v1 - v2)));
        return dot < epsilon ? dot > -epsilon ? Side.On : Side.Back : Side.Front;
    }

    // TODO: Use for insertplane
    private static bool PlanesAreEqual(Plane p1, Plane p2)
    {
        const float epsilon = 0.001f;
        return Vector3.Dot(p1.Normal, p2.Normal) > 1 - epsilon && float.Abs(p1.D - p2.D) <= epsilon;
    }

    // TODO: Use for insertvertex
    private static bool VerticesAreEqual(Vector3 v1, Vector3 v2)
    {
        const float epsilon = 0.001f;
        return (v1 - v2).LengthSquared() < epsilon;
    }
}