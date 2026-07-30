namespace KeepersCompound.Formats.TagFile.Blocks;

public class BlockHeader
{
    public required string Tag { get; set; }
    public required Version Version { get; set; }
}