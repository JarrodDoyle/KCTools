namespace KeepersCompound.Formats.TagFile.Blocks.LmParams;

public class LmParamsBlockParser : IBinaryParser<AbstractBlock>
{
    public AbstractBlock Read(BinaryReader reader)
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

    public void Write(BinaryWriter writer, AbstractBlock item)
    {
        if (item is not LmParamsBlock block)
        {
            return;
        }

        new BlockHeaderParser().Write(writer, block.Header);
        writer.Write(block.DataSize);
        writer.Write(block.Attenuation);
        writer.Write(block.Saturation);
        writer.Write((uint)block.ShadowType);
        writer.Write((uint)block.ShadowSoftness);
        writer.Write(block.CenterWeight);
        writer.Write((uint)block.ShadowDepth);
        writer.Write(block.LightmappedWater);
        writer.Write(new byte[3]);
        writer.Write(block.LightmapScale);
        writer.Write(block.AnimLightCutoff);
    }
}