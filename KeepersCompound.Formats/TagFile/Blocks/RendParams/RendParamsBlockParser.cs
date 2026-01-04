using System.Numerics;

namespace KeepersCompound.Formats.TagFile.Blocks.RendParams;

public class RendParamsBlockParser : IBinaryParser<AbstractBlock>
{
    public AbstractBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        var palette = reader.ReadNullString(16);
        var ambientLight = reader.ReadVec3();
        var useSunlight = reader.ReadBoolean();
        reader.ReadBytes(3);
        var sunlightMode = (SunlightMode)reader.ReadUInt32();
        var sunlightDirection = reader.ReadVec3();
        var sunlightHue = reader.ReadSingle();
        var sunlightSaturation = reader.ReadSingle();
        var sunlightBrightness = reader.ReadSingle();
        var unknown1 = reader.ReadBytes(24);
        var viewDistance = reader.ReadSingle();
        var unknown2 = reader.ReadBytes(12);
        var ambientLightZones = new Vector3[8];
        for (var i = 0; i < ambientLightZones.Length; i++)
        {
            ambientLightZones[i] = reader.ReadVec3();
        }

        var globalAiVisBias = reader.ReadSingle();
        var ambientZoneAiVisBiases = new float[8];
        for (var i = 0; i < ambientZoneAiVisBiases.Length; i++)
        {
            ambientZoneAiVisBiases[i] = reader.ReadSingle();
        }

        return new RendParamsBlock
        {
            Header = header,
            Palette = palette,
            AmbientLight = ambientLight,
            UseSunlight = useSunlight,
            SunlightMode = sunlightMode,
            SunlightDirection = sunlightDirection,
            SunlightHue = sunlightHue,
            SunlightSaturation = sunlightSaturation,
            SunlightBrightness = sunlightBrightness,
            ViewDistance = viewDistance,
            AmbientLightZones = ambientLightZones,
            GlobalAiVisBias = globalAiVisBias,
            AmbientZoneAiVisBiases = ambientZoneAiVisBiases,
            Unknown1 = unknown1,
            Unknown2 = unknown2
        };
    }

    public void Write(BinaryWriter writer, AbstractBlock item)
    {
        if (item is not RendParamsBlock block)
        {
            return;
        }

        new BlockHeaderParser().Write(writer, block.Header);
        writer.WriteNullString(block.Palette, 16);
        writer.WriteVec3(block.AmbientLight);
        writer.Write(block.UseSunlight);
        writer.Write(new byte[3]);
        writer.Write((uint)block.SunlightMode);
        writer.WriteVec3(block.SunlightDirection);
        writer.Write(block.SunlightHue);
        writer.Write(block.SunlightSaturation);
        writer.Write(block.SunlightBrightness);
        writer.Write(block.Unknown1);
        writer.Write(block.ViewDistance);
        writer.Write(block.Unknown2);
        foreach (var lightZone in block.AmbientLightZones)
        {
            writer.WriteVec3(lightZone);
        }

        writer.Write(block.GlobalAiVisBias);
        foreach (var visBias in block.AmbientZoneAiVisBiases)
        {
            writer.Write(visBias);
        }
    }
}