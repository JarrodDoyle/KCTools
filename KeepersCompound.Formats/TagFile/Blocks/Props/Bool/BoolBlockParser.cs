namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BoolBlockParser<T> : AbstractPropBlockParser<T> where T : BoolProp, IFromType<T, BoolProp>
{
    public BoolBlockParser(TocEntry entry) : base(entry)
    {
    }

    protected override IBinaryParser<T> GetPropParser(Version version)
    {
        return version.Minor switch
        {
            4 => new BoolProp4Parser<T>(),
            _ => throw new ArgumentException($"Unrecognised {Entry.Tag} block version {version.Minor}"),
        };
    }
}