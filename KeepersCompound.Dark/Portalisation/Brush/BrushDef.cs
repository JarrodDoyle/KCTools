using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Maths;

namespace KeepersCompound.Dark.Portalisation.Brush;

public class BrushDef
{
    public int Time;
    public Media Operation;
    public Vector3 Translation;
    public Vector3 Rotation;
    public List<BrushDefFace> Faces;

    public BrushDef(int time, Media operation, Vector3 translation, Vector3 rotation, List<BrushDefFace> faces)
    {
        Time = time;
        Operation = operation;
        Translation = translation;
        Rotation = rotation;
        Faces = faces;
    }

    public List<TreeInsertionPoly> BuildInsertionPolys(float worldSize)
    {
        var polys = new List<TreeInsertionPoly>();
        for (var i = 0; i < Faces.Count; i++)
        {
            var winding = new Winding(Faces[i].Plane, worldSize);
            for (var j = 0; j < Faces.Count; j++)
            {
                if (j == i) continue;
                winding.Clip(Faces[j].Plane);
            }

            polys.Add(new TreeInsertionPoly(Faces[i].Plane, winding, false, (Time - 1, i)));
        }

        // Transform planes and windings
        var translation = Matrix4x4.CreateTranslation(Translation);
        var rotation = Matrix4x4.Identity;
        rotation *= Matrix4x4.CreateRotationX(float.DegreesToRadians(Rotation.X));
        rotation *= Matrix4x4.CreateRotationY(float.DegreesToRadians(Rotation.Y));
        rotation *= Matrix4x4.CreateRotationZ(float.DegreesToRadians(Rotation.Z));
        var transform = rotation * translation;
        foreach (var poly in polys)
        {
            poly.Plane = Plane.Transform(poly.Plane, transform);
            var winding = poly.Winding;
            for (var j = 0; j < winding.Vertices.Count; j++)
            {
                winding.Vertices[j] = Vector3.Transform(winding.Vertices[j], transform);
            }
        }

        return polys;
    }
}