using KeepersCompound.Dark.Maths;
using KeepersCompound.Portalisation.Brush;

namespace KeepersCompound.Portalisation;

public class TreeInsertionPoly
{
    public bool UsedForSplit { get; set; }
    public int Plane { get; }
    public Winding Winding { get; }
    public BrushTexInfo TexInfo { get; }

    public TreeInsertionPoly(bool usedForSplit, int plane, Winding winding, BrushTexInfo texInfo)
    {
        UsedForSplit = usedForSplit;
        Plane = plane;
        Winding = winding;
        TexInfo = texInfo;
    }
}