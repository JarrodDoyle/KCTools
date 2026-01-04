namespace KeepersCompound.Formats.TagFile.Blocks.Props.Position;

public class PositionBlockParser: AbstractPropBlockParser<PositionProp>
{
    public PositionBlockParser(TocEntry entry) : base(entry)
    {
    }

    protected override IBinaryParser<PositionProp> GetPropParser(Version version)
    {
        return version.Minor switch
        {
            65558 => new PositionProp65558Parser(),
            _ => throw new ArgumentException($"Unrecognised {Entry.Tag} block version {version.Minor}"),
        };
    }
}