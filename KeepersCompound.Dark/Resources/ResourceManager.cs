using System.Diagnostics.CodeAnalysis;
using System.Text;
using KeepersCompound.Dark.Database;
using KeepersCompound.Formats.Model;
using Serilog;

namespace KeepersCompound.Dark.Resources;

public class ResourceManager
{
    private static readonly string[] TextureExtensions = [".dds", ".png", ".tga", ".pcx", ".gif", ".bmp", ".cel"];

    public InstallContext Context { get; }
    public string ActiveCampaign { get; private set; }

    private readonly VirtualFileSystem _vfs;
    private readonly ResourceSet _omResources;
    private readonly Dictionary<string, ResourceSet> _fmResources;
    private readonly Dictionary<string, ModelFile> _modelCache;

    public ResourceManager(InstallContext context)
    {
        ActiveCampaign = "";
        Context = context;
        _vfs = new VirtualFileSystem();
        _omResources = LoadResources("", Context.LoadPaths, Context.ResPaths);
        _fmResources = new Dictionary<string, ResourceSet>();
        _modelCache = new Dictionary<string, ModelFile>(StringComparer.OrdinalIgnoreCase);
    }

    public void Reset()
    {
        _vfs.Reset();
        _modelCache.Clear();
    }

    public bool SetActiveCampaign(string campaignName)
    {
        if (campaignName == "" || _fmResources.ContainsKey(campaignName) || LoadCampaign(campaignName))
        {
            ActiveCampaign = campaignName;
            return true;
        }

        return false;
    }

    public bool LoadCampaign(string campaignName)
    {
        if (!Context.Fms.Contains(campaignName) || _fmResources.ContainsKey(campaignName))
        {
            return false;
        }

        Log.Information("Loading campaign: {CampaignName}", campaignName);
        var fmDir = Path.Join(Context.FmsDir, campaignName);
        _fmResources.Add(campaignName, LoadResources("FMs/{campaignName}", [fmDir], [fmDir]));
        return true;
    }

    public HashSet<string> GetDbFileNames()
    {
        return ActiveCampaign == "" ? _omResources.DbFiles : _fmResources[ActiveCampaign].DbFiles;
    }

    public HashSet<string> GetTextureNames()
    {
        return ActiveCampaign == "" ? _omResources.Textures : _fmResources[ActiveCampaign].Textures;
    }

    public HashSet<string> GetModelNames()
    {
        return ActiveCampaign == "" ? _omResources.Models : _fmResources[ActiveCampaign].Models;
    }

    public HashSet<string> GetModelTextureNames()
    {
        return ActiveCampaign == "" ? _omResources.ModelTextures : _fmResources[ActiveCampaign].ModelTextures;
    }

    public bool TryGetModel(string modelName, [MaybeNullWhen(false)] out ModelFile modelFile)
    {
        var omModelPath = $"obj/{modelName}.bin";
        var fmModelPath = $"FMs/{ActiveCampaign}/{omModelPath}";
        var fmCachePath = $"{ActiveCampaign}/{modelName}";
        return (ActiveCampaign != "" && TryGetModelInternal(fmCachePath, fmModelPath, out modelFile)) ||
               TryGetModelInternal(modelName, omModelPath, out modelFile);
    }

    public bool TryGetDbFile(string dbFileName, [MaybeNullWhen(false)] out DbFile dbFile)
    {
        if (_vfs.TryGetFileMemoryStream($"FMs/{ActiveCampaign}/{dbFileName}", out var stream) ||
            _vfs.TryGetFileMemoryStream(dbFileName, out stream))
        {
            Log.Information("Loading DbFile: {VirtualPath}", dbFileName);
            using BinaryReader reader = new(stream, Encoding.UTF8, false);
            dbFile = new DbFile(reader);
            return true;
        }

        Log.Error("Failed to load DbFile. File does not exist.");
        dbFile = null;
        return false;
    }

    public bool TryGetDbFileVirtualPath(string dbFileName, out string virtualPath)
    {
        virtualPath = $"FMs/{ActiveCampaign}/{dbFileName}";
        if (ActiveCampaign != "" && _vfs.FileExists(virtualPath))
        {
            return true;
        }

        virtualPath = dbFileName;
        return _vfs.FileExists(virtualPath);
    }

    public bool TryGetObjectTextureVirtualPath(string textureName, out string virtualPath)
    {
        var paths = new[]
        {
            $"FMs/{ActiveCampaign}/obj/txt16", $"FMs/{ActiveCampaign}/obj/txt",
            "obj/txt16", "obj/txt"
        };
        foreach (var prefix in paths)
        {
            foreach (var ext in TextureExtensions)
            {
                virtualPath = $"{prefix}/{textureName}{ext}";
                if (_vfs.FileExists(virtualPath))
                {
                    return true;
                }
            }
        }

        virtualPath = "";
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

    private ResourceSet LoadResources(
        string mountPrefix,
        List<string> loadPaths,
        List<string> resPaths)
    {
        for (var i = 0; i < loadPaths.Count; i++)
        {
            var path = loadPaths[^(i + 1)];
            _vfs.Mount(mountPrefix, path, [".mis", ".gam", ".cow"], false);
        }

        var resSearchOptions = new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive
        };
        for (var i = 0; i < resPaths.Count; i++)
        {
            var resPath = resPaths[^(i + 1)];
            Log.Debug("ResPath: {p}", resPath);
            foreach (var path in Directory.GetFileSystemEntries(resPath, "*", resSearchOptions))
            {
                var name = Path.GetFileName(path).ToLower();
                switch (name)
                {
                    case "fam":
                    case "obj":
                        _vfs.Mount(Path.Join(mountPrefix, name), path, true);
                        break;
                    case "fam.crf":
                    case "obj.crf":
                        _vfs.Mount(mountPrefix, path, true);
                        break;
                }
            }
        }

        var dbFiles = _vfs.GetFilesInFolder(mountPrefix, [".mis", ".cow", ".gam"], false);
        var textures = _vfs.GetFilesInFolder(Path.Join(mountPrefix, "fam"), TextureExtensions, true);
        var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _vfs.GetFilesInFolder(Path.Join(mountPrefix, "obj"), [".bin"], false).ToList()
            .ForEach(path => models.Add(Path.GetFileNameWithoutExtension(path)));
        var modelTextures = _vfs.GetFilesInFolder(Path.Join(mountPrefix, "obj/txt"), TextureExtensions, false);
        modelTextures.UnionWith(_vfs.GetFilesInFolder(Path.Join(mountPrefix, "obj/txt16"), TextureExtensions, false));

        Log.Information("Loaded {DbFiles} mis/gam/cow, {Textures} textures, {Objects} objects, {ObjectTextures} object textures", dbFiles.Count, textures.Count, models.Count, modelTextures.Count);
        Log.Information("Virtual file system has {Count} files", _vfs.FileCount);

        return new ResourceSet
        {
            DbFiles = dbFiles,
            Textures = textures,
            Models = models,
            ModelTextures = modelTextures
        };
    }

    private bool TryGetModelInternal(
        string cachePath,
        string modelPath,
        [MaybeNullWhen(false)] out ModelFile modelFile)
    {
        if (_modelCache.TryGetValue(cachePath, out modelFile))
        {
            return true;
        }

        if (!_vfs.TryGetFileMemoryStream(modelPath, out var stream))
        {
            return false;
        }

        using BinaryReader reader = new(stream, Encoding.UTF8, false);
        var parser = new ModelFileParser();
        modelFile = parser.Read(reader);
        if (modelFile == null)
        {
            return false;
        }

        _modelCache.Add(cachePath, modelFile);
        return true;
    }
}