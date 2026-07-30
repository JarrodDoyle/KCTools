using System.Numerics;

namespace KeepersCompound.Portalisation.Brush;

public struct BrushDefFace
{
    public Plane Plane { get; }
    public BrushTexInfo TexInfo { get; }

    public BrushDefFace(Plane plane, BrushTexInfo texInfo)
    {
        Plane = plane;
        TexInfo = texInfo;
    }
}