namespace KeepersCompound.Formats.Images;

public class DarkImage
{
    public int Width { get; }
    public int Height { get; }
    public MemoryStream Stream { get; }

    public DarkImage(int width, int height, MemoryStream stream)
    {
        Width = width;
        Height = height;
        Stream = stream;
    }
}