namespace KeepersCompound.Formats.TagFile;

public record Version(uint Major, uint Minor)
{
    public override string ToString()
    {
        return $"{Major}.{Minor}";
    }
}