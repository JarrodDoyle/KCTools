namespace KeepersCompound.Formats.TagFile.Blocks.Unknown;

public class UnknownBlock : AbstractBlock
{
    public required byte[] Data { get; set; } = [];
}