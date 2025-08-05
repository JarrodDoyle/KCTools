namespace KeepersCompound.Dark.Resources;

public class OsVirtualFile : BaseVirtualFile
{
    public string OsPath { get; init; }

    public OsVirtualFile(string virtualPath, string osPath) : base(virtualPath)
    {
        OsPath = osPath;
    }

    public override MemoryStream GetMemoryStream()
    {
        var bytes = File.ReadAllBytes(OsPath);
        return new MemoryStream(bytes, 0, bytes.Length, true, true);
    }
}