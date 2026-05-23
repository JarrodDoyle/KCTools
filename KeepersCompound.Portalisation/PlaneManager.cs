using System.Numerics;
using KeepersCompound.Dark.Maths;

namespace KeepersCompound.Portalisation;

public class PlaneManager
{
    private List<Plane> Planes { get; } = [];
    private List<Plane> ReversePlanes { get; } = [];

    /// <summary>
    /// Adds a plane to the plane list if it doesn't already exist and gets it's ID.
    /// </summary>
    /// <param name="plane">The plane to be added</param>
    /// <returns>The ID of the added plane. Negative index indicates the inverse of an existing plane.</returns>
    public int AddPlane(Plane plane)
    {
        // No 0 ID exists, for convenience of negation
        var reversePlane = plane.Inverse();
        for (var i = 0; i < Planes.Count; i++)
        {
            if (plane.EqualsEpsilon(Planes[i], 0.01f)) return i + 1;
            if (reversePlane.EqualsEpsilon(ReversePlanes[i], 0.01f)) return -(i + 1);
        }

        Planes.Add(plane);
        ReversePlanes.Add(reversePlane);
        return Planes.Count;
    }

    /// <summary>
    /// Get a managed plane
    /// </summary>
    /// <param name="id">ID of the plane to get</param>
    /// <returns>The plane with matching ID</returns>
    public Plane GetPlane(int id)
    {
        return id > 0 ? Planes[id - 1] : ReversePlanes[-id - 1];
    }
}