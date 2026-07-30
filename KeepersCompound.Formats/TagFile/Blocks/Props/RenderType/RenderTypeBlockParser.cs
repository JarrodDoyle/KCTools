namespace KeepersCompound.Formats.TagFile.Blocks.Props.RenderType;

public class RenderTypeBlockParser : AbstractPropBlockParser<RenderTypeProp>
{
    public RenderTypeBlockParser(TocEntry entry) : base(entry)
    {
    }

    protected override IBinaryParser<RenderTypeProp> GetPropParser(Version version)
    {
        return version.Minor switch
        {
            4 => new RenderTypeProp4Parser(),
            _ => throw new ArgumentException($"Unrecognised {Entry.Tag} block version {version.Minor}"),
        };
    }
}