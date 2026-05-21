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

    public static bool EqualsEpsilon(this Plane p1, Plane p2, float epsilon = 0.00001f)
    {
        return Vector3.Dot(p1.Normal, p2.Normal) > 1 - epsilon && float.Abs(p1.D - p2.D) <= epsilon;
    }

    public static bool EqualsEpsilon(this Vector3 v1, Vector3 v2, float epsilon = 0.00001f)
    {
        return (v1 - v2).LengthSquared() < epsilon;
    }
}