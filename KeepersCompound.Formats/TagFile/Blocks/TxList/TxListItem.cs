namespace KeepersCompound.Formats.TagFile.Blocks.TxList;

public class TxListItem
{
    public required byte[] Tokens { get; set; }
    public required string Name { get; set; }
}