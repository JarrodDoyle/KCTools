namespace KeepersCompound.Formats.TagFile.Blocks;

public class BlockHeaderParser : IBinaryParser<BlockHeader>
{
    public BlockHeader Read(BinaryReader reader)
    {
        var tag = reader.ReadNullString(12);
        var version = new VersionParser().Read(reader);
        reader.ReadBytes(4);
        return new BlockHeader
        {
            Tag = tag,
            Version = version,
        };
    }

    public void Write(BinaryWriter writer, BlockHeader item)
    {
        writer.WriteNullString(item.Tag, 12);
        new VersionParser().Write(writer, item.Version);
        writer.Write(new byte[4]);
    }
}