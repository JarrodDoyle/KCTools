using System.Diagnostics.CodeAnalysis;
using System.Text;
using KeepersCompound.Dark.Database;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.Dark.Resources;

public class ResourceManager
{
    private static string[] _textureExtensions = [".dds", ".png", ".tga", ".pcx", ".gif", ".bmp", ".cel"];

    public HashSet<string> DbFileNames { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> TextureNames { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All model filenames in current resource context excluding extension.
    /// </summary>
    public HashSet<string> ModelNames { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ModelTextureNames { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    private VirtualFileSystem _vfs = new();
    private Dictionary<string, ModelFile> _modelCache = new(StringComparer.OrdinalIgnoreCase);

    public void Reset()
    {
        _vfs.Reset();
        _modelCache.Clear();
    }

    public void Initialise(InstallContext context, string? campaignName)
    {
        Reset();

        for (var i = 0; i < context.LoadPaths.Count; i++)
        {
            var path = context.LoadPaths[^(i + 1)];
            _vfs.Mount("", path, [".mis", ".gam", ".cow"], false);
        }

        var resSearchOptions = new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive
        };
        for (var i = 0; i < context.ResPaths.Count; i++)
        {
            var resPath = context.ResPaths[^(i + 1)];
            Log.Debug("ResPath: {p}", resPath);
            foreach (var path in Directory.GetFileSystemEntries(resPath, "*", resSearchOptions))
            {
                var name = Path.GetFileName(path).ToLower();
                switch (name)
                {
                    case "fam":
                    case "obj":
                        _vfs.Mount(name, path, true);
                        break;
                    case "fam.crf":
                    case "obj.crf":
                        _vfs.Mount("", path, true);
                        break;
                }
            }
        }

        if (campaignName != null && context.Fms.Contains(campaignName))
        {
            _vfs.Mount("", Path.Join(context.FmsDir, campaignName), true);
        }

        DbFileNames = _vfs.GetFilesInFolder("", [".mis", ".cow", ".gam"], false);
        TextureNames = _vfs.GetFilesInFolder("fam", _textureExtensions, true);
        ModelNames = [];
        _vfs.GetFilesInFolder("obj", [".bin"], false).ToList()
            .ForEach(path => ModelNames.Add(Path.GetFileNameWithoutExtension(path)));
        ModelTextureNames = _vfs.GetFilesInFolder("obj/txt", _textureExtensions, false);
        ModelTextureNames.UnionWith(_vfs.GetFilesInFolder("obj/txt16", _textureExtensions, false));

        Log.Information("Virtual file system has {Count} files", _vfs.FileCount);
        Log.Information(
            "Found {DbFiles} mis/gam/cow, {Textures} textures, {Objects} objects, {ObjectTextures} object textures",
            DbFileNames.Count, TextureNames.Count, ModelNames.Count, ModelTextureNames.Count);
    }

    public bool TryGetModel(string name, [MaybeNullWhen(false)] out ModelFile model)
    {
        if (_modelCache.TryGetValue(name, out model))
        {
            return true;
        }

        if (_vfs.TryGetFileMemoryStream($"obj/{name}.bin", out var stream))
        {
            using BinaryReader reader = new(stream, Encoding.UTF8, false);
            var parser = new ModelFileParser();
            model = parser.Read(reader);
            if (model == null)
            {
                return false;
            }

            _modelCache.Add(name, model);
            return true;
        }

        return false;
    }

    public bool TryGetDbFile(string name, [MaybeNullWhen(false)] out DbFile mission)
    {
        if (_vfs.TryGetFileMemoryStream(name, out var stream))
        {
            Log.Information("Loading DbFile: {VirtualPath}", name);
            using BinaryReader reader = new(stream, Encoding.UTF8, false);
            mission = new DbFile(reader);
            return true;
        }

        Log.Error("Failed to load DbFile. File does not exist.");
        mission = null;
        return false;
    }

    public bool TryGetFilePath(string virtualPath, out string osPath)
    {
        return _vfs.TryGetFilePath(virtualPath, out osPath);
    }

    public bool TryGetFileMemoryStream(string virtualPath, [MaybeNullWhen(false)] out MemoryStream memoryStream)
    {
        return _vfs.TryGetFileMemoryStream(virtualPath, out memoryStream);
    }

    public bool TryGetObjectTextureVirtualPath(string name, out string virtualPath)
    {
        foreach (var prefix in new[] { "obj/txt16", "obj/txt" })
        {
            foreach (var ext in _textureExtensions)
            {
                virtualPath = $"{prefix}/{name}{ext}";
                if (_vfs.FileExists(virtualPath))
                {
                    return true;
                }
            }
        }

        virtualPath = "";
        return false;
    }
}