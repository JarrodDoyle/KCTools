using System.Numerics;

namespace KeepersCompound.Dark.Maths;

public enum Side
{
    Front,
    On,
    Back,
    Crosses,
}

public class Winding
{
    public List<Vector3> Vertices;

    public Winding()
    {
        Vertices = [];
    }

    public Winding(Plane plane, float worldSize)
    {
        var absNorm = Vector3.Abs(plane.Normal);
        var primaryAxis = absNorm.Z > absNorm.Y && absNorm.Z > absNorm.X ? Vector3.UnitX : Vector3.UnitZ;
        var up = Vector3.Normalize(primaryAxis + plane.Normal * -Vector3.Dot(primaryAxis, plane.Normal));
        var right = Vector3.Cross(up, plane.Normal);
        up *= worldSize;
        right *= worldSize;

        var origin = plane.Normal * -plane.D;
        Vertices =
        [
            origin - right + up,
            origin + right + up,
            origin + right - up,
            origin - right - up,
        ];
    }

    public Winding Reversed()
    {
        var reversed = new Winding();
        reversed.Vertices.AddRange(Vertices);
        reversed.Vertices.Reverse();
        return reversed;
    }

    public void Clip(Plane splitPlane, float epsilon = 0.001f)
    {
        if (Vertices.Count == 0)
        {
            return;
        }

        var (clipDistances, clipSides, clipSideCounts) = GetSideDetails(splitPlane, epsilon);
        if (clipSideCounts[(int)Side.Back] == 0 && clipSideCounts[(int)Side.On] != Vertices.Count)
        {
            return;
        }

        if (clipSideCounts[(int)Side.Front] == 0)
        {
            Vertices.Clear();
            return;
        }

        var newVertices = new List<Vector3>();
        ClipInternal(newVertices, clipDistances, clipSides);
        Vertices = newVertices;
    }

    public (Winding, Winding) Split(Plane splitPlane, float epsilon = 0.001f)
    {
        var left = new Winding();
        var right = new Winding();

        if (Vertices.Count == 0)
        {
            return (left, right);
        }

        var (distances, sides, sideCounts) = GetSideDetails(splitPlane, epsilon);
        if (sideCounts[(int)Side.Back] == 0 && sideCounts[(int)Side.On] != Vertices.Count)
        {
            left.Vertices.AddRange(Vertices);
            return (left, right);
        }

        if (sideCounts[(int)Side.Front] == 0)
        {
            right.Vertices.AddRange(Vertices);
            return (left, right);
        }

        ClipInternal(left.Vertices, distances, sides);
        for (var i = 0; i < Vertices.Count; i++)
        {
            sides[i] = sides[i] switch
            {
                Side.Front => Side.Back,
                Side.Back => Side.Front,
                _ => sides[i]
            };
        }

        ClipInternal(right.Vertices, distances, sides);
        return (left, right);
    }

    public (float[], Side[], int[]) GetSideDetails(Plane plane, float epsilon = 0.001f)
    {
        var distances = new float[Vertices.Count];
        var sides = new Side[Vertices.Count];
        var sideCounts = new[] { 0, 0, 0 };
        for (var i = 0; i < Vertices.Count; i++)
        {
            var distance = Vertices[i].DistanceFrom(plane);
            var side = distance > epsilon ? Side.Front : distance < -epsilon ? Side.Back : Side.On;
            distances[i] = distance;
            sides[i] = side;
            sideCounts[(int)side]++;
        }

        return (distances, sides, sideCounts);
    }

    private void ClipInternal(List<Vector3> clippedVertices, float[] distances, Side[] sides)
    {
        for (var i = 0; i < Vertices.Count; i++)
        {
            var i1 = (i + 1) % Vertices.Count;
            var v0 = Vertices[i];
            var v1 = Vertices[i1];
            var side = sides[i];
            var nextSide = sides[i1];

            // Vertices that are inside/on the half-space don't get clipped
            if (sides[i] != Side.Back)
            {
                clippedVertices.Add(v0);
            }

            // We only need to do any clipping if we've swapped from front-to-back or vice versa
            // If either the current or next side is On then that's where we would have clipped to
            // anyway so we also don't need to do anything
            if (side == Side.On || nextSide == Side.On || side == nextSide)
            {
                continue;
            }

            // This is how far along the vector v0 -> v1 the front/back crossover occurs
            var frac = distances[i] / (distances[i] - distances[i1]);
            var splitVertex = v0 + frac * (v1 - v0);
            clippedVertices.Add(splitVertex);
        }

        if (clippedVertices.Count < 3)
        {
            clippedVertices.Clear();
        }
    }
}