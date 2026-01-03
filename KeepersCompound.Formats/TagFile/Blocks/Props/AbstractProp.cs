namespace KeepersCompound.Formats.TagFile.Blocks.Props;

public abstract class AbstractProp
{
    public required int ObjectId { get; set; }
    public required int Length { get; set; }
}