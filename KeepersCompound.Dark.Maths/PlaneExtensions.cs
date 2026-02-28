using System.Numerics;

namespace KeepersCompound.Dark.Maths;

public static class PlaneExtensions
{
    public static float DistanceFrom(this Vector3 point, Plane plane)
    {
        return (Vector3.Dot(plane.Normal, point) + plane.D) / plane.Normal.Length();
    }

    public static Plane Inverse(this Plane plane)
    {
        return new Plane(-plane.Normal, -plane.D);
    }
}