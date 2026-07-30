namespace KeepersCompound.Formats.TagFile.Blocks.Props.Door.TransDoor;

public class TransDoorBlockParser : AbstractPropBlockParser<TransDoorProp>
{
    public TransDoorBlockParser(TocEntry entry) : base(entry)
    {
    }

    protected override IBinaryParser<TransDoorProp> GetPropParser(Version version)
    {
        return version.Minor switch
        {
            66536 => new TransDoorProp66536Parser(),
            66537 => new TransDoorProp66537Parser(),
            66538 => new TransDoorProp66538Parser(),
            _ => throw new ArgumentException($"Unrecognised {Entry.Tag} block version {version.Minor}"),
        };
    }
}