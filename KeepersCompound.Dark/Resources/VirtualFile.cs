using System.IO.Compression;

namespace KeepersCompound.Dark.Resources;

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