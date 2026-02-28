using System.Numerics;
using KeepersCompound.Dark.Maths;

namespace KeepersCompound.Dark.Portalisation;

public class BspPoly
{
    public Plane Plane;
    public Winding Winding;
    public BspNode? LeftNode;
    public BspNode? RightNode;
    public bool Coplanar;

    public BspPoly(Plane plane,
        Winding winding,
        BspNode? leftNode = null,
        BspNode? rightNode = null,
        bool coplanar = false)
    {
        Plane = plane;
        Winding = winding;
        LeftNode = leftNode;
        RightNode = rightNode;
        Coplanar = coplanar;
    }
}