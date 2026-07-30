using KeepersCompound.Dark.Maths;
using KeepersCompound.Portalisation.Brush;

namespace KeepersCompound.Portalisation;

public class TreeExtractionPoly
{
    public int Plane { get; }
    public Winding Winding { get; }
    public BrushTexInfo TexInfo { get; set; }
    public BspNode? LeftNode { get; }
    public BspNode? RightNode { get; }

    public TreeExtractionPoly(int plane, Winding winding, BrushTexInfo texInfo, BspNode? leftNode, BspNode? rightNode)
    {
        Plane = plane;
        Winding = winding;
        TexInfo = texInfo;
        LeftNode = leftNode;
        RightNode = rightNode;
    }

    public TreeExtractionPoly Reversed()
    {
        return new TreeExtractionPoly(-Plane, Winding.Reversed(), TexInfo, RightNode, LeftNode);
    }
}