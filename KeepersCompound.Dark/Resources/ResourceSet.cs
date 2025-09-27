namespace KeepersCompound.Dark.Resources;

public class ResourceSet
{
    public required HashSet<string> DbFiles { get; init; }
    public required HashSet<string> Textures { get; init; }
    public required HashSet<string> Models { get; init; }
    public required HashSet<string> ModelTextures { get; init; }
}