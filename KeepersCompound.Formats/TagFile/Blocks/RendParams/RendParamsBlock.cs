using System.Numerics;

namespace KeepersCompound.Formats.TagFile.Blocks.RendParams;

public class RendParamsBlock : AbstractBlock
{
    public required string Palette { get; set; }
    public required Vector3 AmbientLight { get; set; }
    public required bool UseSunlight { get; set; }
    public required SunlightMode SunlightMode { get; set; }
    public required Vector3 SunlightDirection { get; set; }
    public required float SunlightHue { get; set; }
    public required float SunlightSaturation { get; set; }
    public required float SunlightBrightness { get; set; }
    public required float ViewDistance { get; set; }
    public required Vector3[] AmbientLightZones { get; set; }
    public required float GlobalAiVisBias { get; set; }
    public required float[] AmbientZoneAiVisBiases { get; set; }
    public required byte[] Unknown1 { get; set; }
    public required byte[] Unknown2 { get; set; }
}