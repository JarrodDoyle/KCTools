using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;

namespace KeepersCompound.Lighting;

public struct LightMapperSettings
{
    public Vector3[] AmbientLight;
    public bool Hdr;
    public float Attenuation;
    public float Saturation;
    public SoftnessMode MultiSampling;
    public float MultiSamplingCenterWeight;
    public bool LightmappedWater;
    public LightMapperSunSettings Sunlight;
    public uint AnimLightCutoff;
}