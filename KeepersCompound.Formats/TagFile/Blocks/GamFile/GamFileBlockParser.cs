namespace KeepersCompound.Formats.TagFile.Blocks.GamFile;

public class GamFileBlockParser : IBinaryParser<GamFileBlock>
{
    public GamFileBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        var fileName = reader.ReadNullString(256);

        return new GamFileBlock
        {
            Header = header,
            FileName = fileName,
        };
    }

    public void Write(BinaryWriter writer, GamFileBlock item)
    {
        new BlockHeaderParser().Write(writer, item.Header);
        writer.WriteNullString(item.FileName, 256);
    }
}