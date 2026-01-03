namespace KeepersCompound.Formats.TagFile.Blocks.Props.Door.RotDoor;

public class RotDoorBlockParser : AbstractPropBlockParser<RotDoorProp>
{
    public RotDoorBlockParser(TocEntry entry) : base(entry)
    {
    }

    protected override IBinaryParser<RotDoorProp> GetPropParser(Version version)
    {
        return version.Minor switch
        {
            66537 => new RotDoorProp66537Parser(),
            66538 => new RotDoorProp66538Parser(),
            66539 => new RotDoorProp66539Parser(),
            _ => throw new ArgumentException($"Unrecognised {Entry.Tag} block version {version.Minor}"),
        };
    }
}