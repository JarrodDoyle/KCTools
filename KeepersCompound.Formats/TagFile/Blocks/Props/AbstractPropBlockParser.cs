
namespace KeepersCompound.Formats.TagFile.Blocks.Props;

public abstract class AbstractPropBlockParser<T> : IBinaryParser<PropBlock<T>> where T: AbstractProp
{
    protected readonly TocEntry Entry;

    protected AbstractPropBlockParser(TocEntry entry)
    {
        Entry = entry;
    }
    
    public PropBlock<T> Read(BinaryReader reader)
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

    public void Write(BinaryWriter writer, PropBlock<T> item)
    {
        new BlockHeaderParser().Write(writer, item.Header);
        var propParser = GetPropParser(item.Header.Version);
        foreach (var prop in item.Props)
        {
            propParser.Write(writer, prop);
        }
    }

    protected abstract IBinaryParser<T> GetPropParser(Version version);
}