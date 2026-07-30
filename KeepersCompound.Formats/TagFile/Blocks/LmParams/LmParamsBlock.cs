namespace KeepersCompound.Formats.TagFile.Blocks.LmParams;

public class LmParamsBlock : AbstractBlock
{
    public int DataSize { get; set; }
    public float Attenuation { get; set; }
    public float Saturation { get; set; }
    public LightingMode ShadowType { get; set; }
    public SoftnessMode ShadowSoftness { get; set; }
    public float CenterWeight { get; set; }
    public DepthMode ShadowDepth { get; set; }
    public bool LightmappedWater { get; set; }
    public int LightmapScale { get; set; }
    public uint AnimLightCutoff { get; set; }
}