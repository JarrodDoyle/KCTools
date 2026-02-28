using System.Numerics;

namespace KeepersCompound.Dark.Maths;

public class Aabb
{
    public Vector3 Min = new(float.MaxValue, float.MaxValue, float.MaxValue);
    public Vector3 Max = new(float.MinValue, float.MinValue, float.MinValue);

    public void AddPoints(IEnumerable<Vector3> points)
    {
        foreach (var p in points)
        {
            AddPoint(p);
        }
    }

    public void AddPoint(Vector3 p)
    {
        Min = Vector3.Min(Min, p);
        Max = Vector3.Max(Max, p);
    }
}