using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Maths;
using Serilog;

namespace KeepersCompound.Dark.Portalisation.Brush;

public static class BrushListBuilder
{
    private enum PrimitiveType
    {
        Cube,
        Cylinder,
        Pyramid,
        CornerPyramid,
        Wedge,
        Dodecahedron,
    }

    private record BrushShape(PrimitiveType Primitive, int SideCount, bool SideAligned);

    public static List<BrushDef> FromChunk(BrList chunk)
    {
        var brushes = new List<BrushDef>();
        foreach (var chunkBrush in chunk.Brushes)
        {
            if (chunkBrush.Media > Media.Blockable)
            {
                continue;
            }

            var shape = GetShape(chunkBrush);
            var brush = shape.Primitive switch
            {
                PrimitiveType.Cube => BuildCube(chunkBrush),
                PrimitiveType.Wedge => BuildWedge(chunkBrush),
                PrimitiveType.Cylinder => BuildCylinder(chunkBrush, shape.SideCount, shape.SideAligned),
                PrimitiveType.Pyramid => BuildPyramid(chunkBrush, shape.SideCount, false, shape.SideAligned),
                PrimitiveType.CornerPyramid => BuildPyramid(chunkBrush, shape.SideCount, true, shape.SideAligned),
                _ => null,
            };
            if (brush != null)
            {
                brushes.Add(brush);
            }
            else
            {
                Log.Information("Unhandled brush: {P}", shape.Primitive);
            }
        }

        return brushes;
    }

    private static BrushDef BuildCube(BrList.Brush chunkBrush)
    {
        // TODO: Texture info
        var size = chunkBrush.Size;
        List<BrushDefFace> faces =
        [
            new(new(1, 0, 0, size.X), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(0, 1, 0, size.Y), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(-1, 0, 0, size.X), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(0, -1, 0, size.Y), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(0, 0, -1, size.Z), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(0, 0, 1, size.Z), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
        ];

        return new BrushDef(chunkBrush.Time, chunkBrush.Media, chunkBrush.Position, chunkBrush.Angle, faces);
    }

    private static BrushDef BuildWedge(BrList.Brush chunkBrush)
    {
        // TODO: Texture info
        var size = chunkBrush.Size;
        List<BrushDefFace> faces =
        [
            new(new(0, -size.Z, -size.Y, 0), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(0, 0, 1, size.Z), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(0, 1, 0, size.Y), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(-1, 0, 0, size.X), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
            new(new(1, 0, 0, size.X), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero),
        ];

        return new BrushDef(chunkBrush.Time, chunkBrush.Media, chunkBrush.Position, chunkBrush.Angle, faces);
    }

    private static BrushDef BuildCylinder(BrList.Brush chunkBrush, int sides, bool faceAlign)
    {
        // TODO: Texture info
        var size = chunkBrush.Size;
        var faces = new List<BrushDefFace>();
        var points = GetNgonPoints(sides, size.AsVector2(), faceAlign);
        for (var i = 0; i < sides; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % sides];
            var center = new Vector3((p1 + p2) / 2, 0);
            var norm = Vector3.Normalize(new Vector3(p1.Y - p2.Y, p2.X - p1.X, 0));
            var plane = new Plane(norm, -Vector3.Dot(center, norm));
            faces.Add(new(plane, 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero));
        }

        faces.Add(new(new(0, 0, -1, size.Z), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero));
        faces.Add(new(new(0, 0, 1, size.Z), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero));
        return new BrushDef(chunkBrush.Time, chunkBrush.Media, chunkBrush.Position, chunkBrush.Angle, faces);
    }

    private static BrushDef BuildPyramid(BrList.Brush chunkBrush, int sides, bool corner, bool faceAlign)
    {
        // TODO: Texture info
        var size = chunkBrush.Size;
        var faces = new List<BrushDefFace>();
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
            var plane = new Plane(norm, -Vector3.Dot(top, norm));
            faces.Add(new(plane, 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero));
        }

        faces.Add(new(new(0, 0, 1, size.Z), 0, Vector3.One, Vector3.One, 1, 1, 0, Vector2.Zero));
        return new BrushDef(chunkBrush.Time, chunkBrush.Media, chunkBrush.Position, chunkBrush.Angle, faces);
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
}