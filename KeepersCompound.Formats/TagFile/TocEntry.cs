namespace KeepersCompound.Formats.TagFile;

public record TocEntry(string Tag, uint Offset, uint Size)
{
    public override string ToString()
    {
        return $"Tag: {Tag}, Offset: {Offset}, Size: {Size}";
    }
}