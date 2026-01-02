namespace KeepersCompound.Formats.TagFile.Blocks.Props.Door.RotDoor;

public class RotDoorBlockParser : IBinaryParser<RotDoorBlock>
{
    private readonly TocEntry _entry;

    public RotDoorBlockParser(TocEntry entry)
    {
        _entry = entry;
    }

    public RotDoorBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        IBinaryParser<RotDoorProp> propParser = header.Version.Minor switch
        {
            66537 => new RotDoorProp66537Parser(),
            66538 => new RotDoorProp66538Parser(),
            66539 => new RotDoorProp66539Parser(),
            _ => throw new ArgumentException($"Unrecognised P$RotDoor block version {header.Version.Minor}"),
        };

        var props = new List<RotDoorProp>();
        while (reader.BaseStream.Position < _entry.Offset + _entry.Size + 24)
        {
            props.Add(propParser.Read(reader) ?? throw new InvalidOperationException("Parsed prop is null"));
        }

        return new RotDoorBlock
        {
            Header = header,
            Props = props,
        };
    }

    public void Write(BinaryWriter writer, RotDoorBlock item)
    {
        new BlockHeaderParser().Write(writer, item.Header);
        IBinaryParser<RotDoorProp> propParser = item.Header.Version.Minor switch
        {
            66537 => new RotDoorProp66537Parser(),
            66538 => new RotDoorProp66538Parser(),
            66536 => new RotDoorProp66539Parser(),
            _ => throw new ArgumentException($"Unrecognised P$TransDoor block version {item.Header.Version.Minor}"),
        };
        foreach (var prop in item.Props)
        {
            propParser.Write(writer, prop);
        }
    }
}