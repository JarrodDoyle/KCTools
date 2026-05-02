using System.Numerics;
using KeepersCompound.Dark.Maths;

namespace KeepersCompound.Dark.Portalisation;

public class BspPoly
{
    public Plane Plane;
    public Winding Winding;
    public (int, int) BrushFace;
    public BspNode? LeftNode;
    public BspNode? RightNode;

    public BspPoly(Plane plane,
        Winding winding,
        (int, int) face,
        BspNode? leftNode = null,
        BspNode? rightNode = null)
    {
        Plane = plane;
        Winding = winding;
        BrushFace = face;
        LeftNode = leftNode;
        RightNode = rightNode;
    }
}

public class TreeInsertionPoly
{
    public Plane Plane;
    public Winding Winding;
    public bool Coplanar;
    public (int, int) BrushFace;
    
    public TreeInsertionPoly(Plane plane,
        Winding winding,
        bool coplanar,
        (int, int) face)
    {
        Plane = plane;
        Winding = winding;
        Coplanar = coplanar;
        BrushFace = face;
    }
}