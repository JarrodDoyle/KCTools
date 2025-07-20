using System.IO.Compression;

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

public class ZipVirtualFile : BaseVirtualFile
{
    private readonly ZipArchive _archive;
    private readonly string _entryName;

    public ZipVirtualFile(string virtualPath, ZipArchive archive, string entryName) : base(virtualPath)
    {
        _archive = archive;
        _entryName = entryName;
    }

    public override MemoryStream GetMemoryStream()
    {
        var baseStream = _archive.GetEntry(_entryName)?.Open() ??
                         throw new InvalidOperationException("Entry not found in archive.");
        var memoryStream = new MemoryStream();
        baseStream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
}