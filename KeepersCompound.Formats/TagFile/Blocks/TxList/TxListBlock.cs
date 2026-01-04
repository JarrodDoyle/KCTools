namespace KeepersCompound.Formats.TagFile.Blocks.TxList;

public class TxListBlock : AbstractBlock
{
    public required int BlockSize { get; set; }
    public required int ItemCount { get; set; }
    public required int TokenCount { get; set; }
    public required string[] Tokens { get; set; }
    public required TxListItem[] Items { get; set; }
}