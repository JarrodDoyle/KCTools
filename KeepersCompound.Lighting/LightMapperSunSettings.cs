using System.Numerics;

namespace KeepersCompound.Lighting;

// The objcast element of sunlight is ignored, we just care if it's quadlit
public struct LightMapperSunSettings
{
    public bool Enabled;
    public bool QuadLit;
    public Vector3 Direction;
    public Vector3 Color;
}