using KeepersCompound.Dark.Maths;

namespace KeepersCompound.Portalisation;

public class OptimizePoly
{
    public Winding Winding { get; }
    public bool Flipped { get; }

    public OptimizePoly(Winding winding, bool flipped)
    {
        Winding = winding;
        Flipped = flipped;
    }
}