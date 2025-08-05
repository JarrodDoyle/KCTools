namespace KeepersCompound.Dark.Resources;

public abstract class BaseVirtualFile
{
    public string VirtualPath { get; init; }
    public abstract MemoryStream GetMemoryStream();

    protected BaseVirtualFile(string virtualPath)
    {
        VirtualPath = virtualPath;
    }
}