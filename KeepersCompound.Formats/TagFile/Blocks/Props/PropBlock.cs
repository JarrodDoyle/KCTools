namespace KeepersCompound.Formats.TagFile.Blocks.Props;

public class PropBlock<T> : AbstractBlock where T: AbstractProp
{
    public required List<T> Props { get; set; }
}