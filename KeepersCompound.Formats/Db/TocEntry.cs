namespace KeepersCompound.Formats.Db;

public record TocEntry(string Name, uint Offset, uint Size)
{
    public override string ToString()
    {
        return $"Name: {Name}, Offset: {Offset}, Size: {Size}";
    }
}