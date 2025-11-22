namespace KeepersCompound.Formats.TagFile.Blocks.Unknown;

public class UnknownBlockParser : IBinaryParser<UnknownBlock>
{
    private readonly TocEntry _entry;

    public UnknownBlockParser(TocEntry entry)
    {
        _entry = entry;
    }

    public UnknownBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        var data = reader.ReadBytes((int)_entry.Size);
        return new UnknownBlock
        {
            Header = header,
            Data = data,
        };
    }

    public void Write(BinaryWriter writer, UnknownBlock item)
    {
        new BlockHeaderParser().Write(writer, item.Header);
        writer.Write(item.Data);
    }
}