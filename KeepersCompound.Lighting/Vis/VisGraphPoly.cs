using System.Numerics;

namespace KeepersCompound.Lighting.Vis;

public class VisGraphPoly
{
    public Vector3 Center { get; private set; }
    public float Radius { get; private set; }
    public List<Vector3> Vertices { get; }
    public Plane Plane { get; }

    public VisGraphPoly(Plane plane, List<Vector3> vertices)
    {
        Plane = plane;
        Vertices = vertices;
        ComputeCenterRadius();
    }

    public VisGraphPoly(VisGraphPoly other)
    {
        Center = other.Center;
        Radius = other.Radius;
        Plane = other.Plane;
        Vertices = [..other.Vertices];
    }

    /// <summary>
    /// If vertices are modified then <see cref="Center"/> and <see cref="Radius"/> will be inaccurate and should be
    /// recomputed before being used.
    /// </summary>
    public void ComputeCenterRadius()
    {
        Center = Vector3.Zero;
        foreach (var v in Vertices)
        {
            Center += v;
        }

        Center /= Vertices.Count;

        // Radius is the max vertex distance from the center
        // We're actually calculating radius squared to begin with because it's faster :)
        Radius = 0;
        foreach (var v in Vertices)
        {
            Radius = float.Max(Radius, (v - Center).LengthSquared());
        }

        Radius = MathF.Sqrt(Radius);
    }
}