using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;

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
        foreach (var poly in polys)
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
}