using System.Diagnostics.CodeAnalysis;
using KeepersCompound.Formats.TagFile.Blocks;

namespace KeepersCompound.Formats.TagFile;

public class TagFile
{
    public required uint TocOffset { get; set; }
    public required Version Version { get; set; }
    public required string DeadBeef { get; set; }
    public required List<TocEntry> TocEntries { get; set; }
    public required Dictionary<string, AbstractBlock> Blocks { get; set; }

    public bool TryGetBlock<T>([MaybeNullWhen(false)] out T block) where T : AbstractBlock
    {
        block = (T?)Blocks.Values.FirstOrDefault(e => e is T);
        return block != null;
    }
}