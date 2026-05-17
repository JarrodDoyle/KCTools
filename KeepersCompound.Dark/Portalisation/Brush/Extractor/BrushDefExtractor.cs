using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;
using Serilog;

namespace KeepersCompound.Dark.Portalisation.Brush.Extractor;

public class BrushDefExtractor
{
    public List<BrushDef> BrushDefs { get; }

    public BrushDefExtractor()
    {
        BrushDefs = [];
    }

    public void AddBrushList(List<BrList.Brush> brushes)
    {
        foreach (var brush in brushes)
        {
            if (brush.Media <= Media.Blockable && TryBuildBrush(brush, out var brushDef))
            {
                BrushDefs.Add(brushDef);
            }
        }
    }

    private bool TryBuildBrush(BrList.Brush brush, [NotNullWhen(true)] out BrushDef? brushDef)
    {
        var shape = GetShape(brush);
        var planes = shape.Primitive switch
        {
            PrimitiveType.Cube => GetCubePlanes(brush.Size),
            PrimitiveType.Wedge => GetWedgePlanes(brush.Size),
            PrimitiveType.Cylinder => GetCylinderPlanes(brush.Size, shape.SideCount, shape.SideAligned),
            PrimitiveType.Pyramid => GetPyramidPlanes(brush.Size, shape.SideCount, false, shape.SideAligned),
            PrimitiveType.CornerPyramid => GetPyramidPlanes(brush.Size, shape.SideCount, true, shape.SideAligned),
            _ => [],
        };

        if (planes.Count != brush.Txs.Length)
        {
            Log.Information("Unhandled brush: {Id}, {P}", brush.Id, shape.Primitive);
            brushDef = null;
            return false;
        }

        foreach (var plane in planes)
        {
            if (float.IsNaN(plane.Normal.X) || float.IsNaN(plane.Normal.Y) || float.IsNaN(plane.Normal.Z) ||
                float.IsNaN(plane.D))
            {
                Log.Error("Skipping brush {Id} with invalid plane.", brush.Id);
                brushDef = null;
                return false;
            }
        }

        var projections = GetTextureProjections(planes);

        var translation = Matrix4x4.CreateTranslation(brush.Position);
        var rotation = Matrix4x4.Identity;
        rotation *= Matrix4x4.CreateRotationX(float.DegreesToRadians(brush.Angle.X));
        rotation *= Matrix4x4.CreateRotationY(float.DegreesToRadians(brush.Angle.Y));
        rotation *= Matrix4x4.CreateRotationZ(float.DegreesToRadians(brush.Angle.Z));
        var transform = rotation * translation;

        var faces = new List<BrushDefFace>(planes.Count);
        for (var i = 0; i < planes.Count; i++)
        {
            var plane = Plane.Transform(Plane.Normalize(planes[i]), transform);
            var uProjection = Vector3.Transform(projections[i].Item1, rotation);
            var vProjection = Vector3.Transform(projections[i].Item2, rotation);
            var texId = brush.Txs[i].Id > 0 ? brush.Txs[i].Id : brush.TextureId;
            var offset = new Vector2(brush.Txs[i].X, brush.Txs[i].Y) / 64f;
            var scale = (1 << brush.Txs[i].Scale) * (4f / (1 << 16));
            var texInfo = new BrushTexInfo((uint)texId, uProjection, vProjection, scale, scale, 0, offset);
            faces.Add(new BrushDefFace(plane, texInfo));
        }

        brushDef = new BrushDef((BrushOperation)brush.Media, faces);
        return true;
    }

    private static BrushShape GetShape(BrList.Brush brush)
    {
        var info = brush.BrushInfo;
        var primType = (info & 0xFFFFFE00) >> 9;
        var sideAligned = (info & 0x00000100) >> 8 != 0;
        var extraInfo = (int)(info & 0x000000FF);

        return primType switch
        {
            0 when extraInfo == 1 => new BrushShape(PrimitiveType.Cube, 0, sideAligned),
            0 when extraInfo == 7 => new BrushShape(PrimitiveType.Wedge, 0, sideAligned),
            0 when extraInfo == 6 => new BrushShape(PrimitiveType.Dodecahedron, 0, sideAligned),
            1 => new BrushShape(PrimitiveType.Cylinder, extraInfo + 3, sideAligned),
            2 => new BrushShape(PrimitiveType.Pyramid, extraInfo + 3, sideAligned),
            3 => new BrushShape(PrimitiveType.CornerPyramid, extraInfo + 3, sideAligned),
            _ => new BrushShape(PrimitiveType.Cube, 6, sideAligned)
        };
    }

    private static Vector2[] GetNgonPoints(int sides, Vector2 stretch, bool faceAlign)
    {
        var points = new Vector2[sides];
        if (faceAlign)
        {
            stretch *= 1.0f / MathF.Cos(2 * float.Pi * 0.5f / sides);
        }

        var offset = faceAlign ? 0.5f : 0.0f;
        for (var i = 0; i < sides; i++)
        {
            var angle = 2 * float.Pi * (i + offset) / sides;
            var x = -MathF.Sin(angle) * stretch.X;
            var y = MathF.Cos(angle) * stretch.Y;
            points[i] = new Vector2(x, y);
        }

        return points;
    }

    private static List<Plane> GetCubePlanes(Vector3 size)
    {
        return
        [
            new(1, 0, 0, size.X),
            new(0, 1, 0, size.Y),
            new(-1, 0, 0, size.X),
            new(0, -1, 0, size.Y),
            new(0, 0, -1, size.Z),
            new(0, 0, 1, size.Z)
        ];
    }

    private static List<Plane> GetWedgePlanes(Vector3 size)
    {
        return
        [
            new(0, -size.Z, -size.Y, 0),
            new(0, 0, 1, size.Z),
            new(0, 1, 0, size.Y),
            new(-1, 0, 0, size.X),
            new(1, 0, 0, size.X),
        ];
    }

    private static List<Plane> GetCylinderPlanes(Vector3 size, int sides, bool faceAlign)
    {
        var planes = new List<Plane>(sides + 2);
        var points = GetNgonPoints(sides, size.AsVector2(), faceAlign);
        for (var i = 0; i < sides; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % sides];
            var center = new Vector3((p1 + p2) / 2, 0);
            var norm = Vector3.Normalize(new Vector3(p1.Y - p2.Y, p2.X - p1.X, 0));
            planes.Add(new Plane(norm, -Vector3.Dot(center, norm)));
        }

        planes.Add(new(0, 0, -1, size.Z));
        planes.Add(new(0, 0, 1, size.Z));
        return planes;
    }

    private static List<Plane> GetPyramidPlanes(Vector3 size, int sides, bool corner, bool faceAlign)
    {
        var planes = new List<Plane>(sides + 1);
        var points = GetNgonPoints(sides, size.AsVector2(), faceAlign);
        var top = Vector3.UnitZ * size.Z;
        if (corner)
        {
            top += new Vector3(points[0], 0);
        }

        for (var i = 0; i < sides; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % sides];
            var norm = Vector3.Normalize(Vector3.Cross(top - new Vector3(p2, -size.Z), top - new Vector3(p1, -size.Z)));
            planes.Add(new Plane(norm, -Vector3.Dot(top, norm)));
        }

        planes.Add(new(0, 0, 1, size.Z));
        return planes;
    }

    private readonly Vector3[][] _projectionAxes =
    [
        [Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ],
        [Vector3.UnitY, -Vector3.UnitX, -Vector3.UnitZ],
        [Vector3.UnitZ, Vector3.UnitX, -Vector3.UnitY],
        [-Vector3.UnitX, -Vector3.UnitY, -Vector3.UnitZ],
        [-Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ],
        [-Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY]
    ];

    private List<(Vector3, Vector3)> GetTextureProjections(List<Plane> planes)
    {
        var projections = new List<(Vector3, Vector3)>();
        foreach (var plane in planes)
        {
            var bestIndex = -1;
            var bestSize = 0f;
            for (var i = 0; i < 6; i++)
            {
                var size = Vector3.Dot(_projectionAxes[i][0], plane.Normal);
                if (size > bestSize)
                {
                    bestSize = size;
                    bestIndex = i;
                }
            }

            if (bestIndex == -1)
            {
                Log.Error("Failed to find plane texture mapping");
                projections.Add((Vector3.UnitX, Vector3.UnitY));
                continue;
            }

            projections.Add((_projectionAxes[bestIndex][1], _projectionAxes[bestIndex][2]));
        }

        return projections;
    }
}