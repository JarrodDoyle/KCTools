namespace KeepersCompound.Formats.TagFile.Blocks.Props.Door.TransDoor;

public class TransDoorBlockParser : IBinaryParser<TransDoorBlock>
{
    private readonly TocEntry _entry;

    public TransDoorBlockParser(TocEntry entry)
    {
        _entry = entry;
    }

    public TransDoorBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        IBinaryParser<TransDoorProp> propParser = header.Version.Minor switch
        {
            66536 => new TransDoorProp66536Parser(),
            66537 => new TransDoorProp66537Parser(),
            66538 => new TransDoorProp66538Parser(),
            _ => throw new ArgumentException($"Unrecognised P$TransDoor block version {header.Version.Minor}"),
        };

        var props = new List<TransDoorProp>();
        while (reader.BaseStream.Position < _entry.Offset + _entry.Size + 24)
        {
            props.Add(propParser.Read(reader) ?? throw new InvalidOperationException("Parsed prop is null"));
        }

        return new TransDoorBlock
        {
            Header = header,
            Props = props,
        };
    }

    public void Write(BinaryWriter writer, TransDoorBlock item)
    {
        new BlockHeaderParser().Write(writer, item.Header);
        IBinaryParser<TransDoorProp> propParser = item.Header.Version.Minor switch
        {
            66536 => new TransDoorProp66536Parser(),
            66537 => new TransDoorProp66537Parser(),
            66538 => new TransDoorProp66538Parser(),
            _ => throw new ArgumentException($"Unrecognised P$TransDoor block version {item.Header.Version.Minor}"),
        };
        foreach (var prop in item.Props)
        {
            propParser.Write(writer, prop);
        }
    }
}