using KeepersCompound.Dark.Maths;

namespace KeepersCompound.Dark.Portalisation.Brush;

public class BrushDef
{
    public int Time { get; }
    public BrushOperation Operation { get; }
    public List<BrushDefFace> Faces { get; }

    public BrushDef(int time, BrushOperation operation, List<BrushDefFace> faces)
    {
        Time = time;
        Operation = operation;
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

            polys.Add(new TreeInsertionPoly(false, Faces[i].Plane, winding, Faces[i].TexInfo));
        }

        return polys;
    }
}