namespace KeepersCompound.Formats.TagFile.Blocks.LmParams;

public class LmParamsBlockParser : IBinaryParser<LmParamsBlock>
{
    public LmParamsBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        var dataSize = reader.ReadInt32();
        var attenuation = reader.ReadSingle();
        var saturation = reader.ReadSingle();
        var shadowType = (LightingMode)reader.ReadUInt32();
        var shadowSoftness = (SoftnessMode)reader.ReadUInt32();
        var centerWeight = reader.ReadSingle();
        var shadowDepth = (DepthMode)reader.ReadUInt32();
        var lightmappedWater = reader.ReadBoolean();
        reader.ReadBytes(3);
        var lightmapScale = reader.ReadInt32();
        var animLightCutoff = reader.ReadUInt32();

        return new LmParamsBlock
        {
            Header = header,
            DataSize = dataSize,
            Attenuation = attenuation,
            Saturation = saturation,
            ShadowType = shadowType,
            ShadowSoftness = shadowSoftness,
            CenterWeight = centerWeight,
            ShadowDepth = shadowDepth,
            LightmappedWater = lightmappedWater,
            LightmapScale = lightmapScale,
            AnimLightCutoff = animLightCutoff
        };
    }

    public void Write(BinaryWriter writer, LmParamsBlock item)
    {
        new BlockHeaderParser().Write(writer, item.Header);
        writer.Write(item.DataSize);
        writer.Write(item.Attenuation);
        writer.Write(item.Saturation);
        writer.Write((uint)item.ShadowType);
        writer.Write((uint)item.ShadowSoftness);
        writer.Write(item.CenterWeight);
        writer.Write((uint)item.ShadowDepth);
        writer.Write(item.LightmappedWater);
        writer.Write(new byte[3]);
        writer.Write(item.LightmapScale);
        writer.Write(item.AnimLightCutoff);
    }
}