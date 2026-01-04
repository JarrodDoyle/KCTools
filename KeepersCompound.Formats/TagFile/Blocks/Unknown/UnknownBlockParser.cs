namespace KeepersCompound.Formats.TagFile.Blocks.Unknown;

public class UnknownBlockParser : IBinaryParser<AbstractBlock>
{
    private readonly TocEntry _entry;

    public UnknownBlockParser(TocEntry entry)
    {
        _entry = entry;
    }

    public AbstractBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        var data = reader.ReadBytes((int)_entry.Size);
        return new UnknownBlock
        {
            Header = header,
            Data = data,
        };
    }

    public void Write(BinaryWriter writer, AbstractBlock item)
    {
        if (item is not UnknownBlock block)
        {
            return;
        }

        new BlockHeaderParser().Write(writer, block.Header);
        writer.Write(block.Data);
    }
}