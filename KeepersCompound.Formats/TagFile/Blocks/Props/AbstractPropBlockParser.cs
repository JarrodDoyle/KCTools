namespace KeepersCompound.Formats.TagFile.Blocks.Props;

public abstract class AbstractPropBlockParser<T> : IBinaryParser<AbstractBlock> where T : AbstractProp
{
    protected readonly TocEntry Entry;

    protected AbstractPropBlockParser(TocEntry entry)
    {
        Entry = entry;
    }

    public AbstractBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        var propParser = GetPropParser(header.Version);
        var props = new List<T>();
        while (reader.BaseStream.Position < Entry.Offset + Entry.Size + 24)
        {
            props.Add(propParser.Read(reader) ?? throw new InvalidOperationException("Parsed prop is null"));
        }

        return new PropBlock<T>
        {
            Header = header,
            Props = props,
        };
    }

    public void Write(BinaryWriter writer, AbstractBlock item)
    {
        if (item is not PropBlock<T> block)
        {
            return;
        }

        new BlockHeaderParser().Write(writer, block.Header);
        var propParser = GetPropParser(block.Header.Version);
        foreach (var prop in block.Props)
        {
            propParser.Write(writer, prop);
        }
    }

    protected abstract IBinaryParser<T> GetPropParser(Version version);
}