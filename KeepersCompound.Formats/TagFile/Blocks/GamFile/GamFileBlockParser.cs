namespace KeepersCompound.Formats.TagFile.Blocks.GamFile;

public class GamFileBlockParser : IBinaryParser<AbstractBlock>
{
    public AbstractBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        var fileName = reader.ReadNullString(256);

        return new GamFileBlock
        {
            Header = header,
            FileName = fileName,
        };
    }

    public void Write(BinaryWriter writer, AbstractBlock item)
    {
        if (item is not GamFileBlock block)
        {
            return;
        }

        new BlockHeaderParser().Write(writer, block.Header);
        writer.WriteNullString(block.FileName, 256);
    }
}