using System.Numerics;
using System.Text;
using DotMake.CommandLine;
using ImageMagick;
using KeepersCompound.Dark;
using KeepersCompound.Dark.Resources;
using KeepersCompound.Formats.Model;
using KeepersCompound.Lighting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;
using SharpGLTF.Transforms;

namespace KCTools;

using VERTEX = VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>;

internal static class Program
{
    internal static void ConfigureLogger(bool quiet)
    {
        const string outputTemplate = "{Timestamp:HH:mm:ss.fff} [{Level}] {Message:lj}{NewLine}{Exception}";
        var logPath = $"{AppDomain.CurrentDomain.BaseDirectory}/logs/{DateTime.Now:yyyyMMdd_HHmmss}.log";
        var config = new LoggerConfiguration();
#if DEBUG
        config.MinimumLevel.Debug();
#endif

        if (!quiet)
        {
            config.WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, outputTemplate: outputTemplate);
        }

        config.WriteTo.File(logPath, outputTemplate: outputTemplate);
        Log.Logger = config.CreateLogger();
    }

    public static void Main(string[] args)
    {
        Cli.Run<RootCommand>(args);
    }
}

[CliCommand(Description = "Tools for working with NewDark files.")]
public class RootCommand
{
    [CliCommand(Description = "Compute lightmaps for a NewDark .MIS/.COW")]
    public class LightCommand
    {
        [CliArgument(Description = "The path to the root Thief installation.")]
        public required string InstallPath { get; set; }

        [CliArgument(Description = "Mission filename including extension.")]
        public required string MissionName { get; set; }

        [CliOption(Description = "Fan mission folder name. Uses OMs if not specified.")]
        public string? CampaignName { get; set; } = null;

        [CliOption(Description = "Name of output file excluding extension. Overwrites existing mission if not specified.")]
        public string? OutputName { get; set; } = null;

        [CliOption(Description = "Use a simpler Light to Cell visibility calculation. Only use for debugging.")]
        public bool SimpleVis { get; set; } = false;

        [CliOption(Description = "Report light configuration problems without performing any lighting.")]
        public bool Inspect { get; set; } = false;

        [CliOption(Description = "Disable terminal output.")]
        public bool Quiet { get; set; } = false;

        [CliOption(Description = "Automatically obtain campaign name from `DromEd.log`. Overrides `--campaign-name`.")]
        public bool AutoCampaign { get; set; } = false;

        public void Run()
        {
            Program.ConfigureLogger(Quiet);

            Timing.Reset();
            Timing.TimeStage("Total", () =>
            {
                var context = Timing.TimeStage("Initialise install context", () => new InstallContext(InstallPath));
                if (!context.Valid)
                {
                    Log.Error("Invalid install context");
                    return;
                }

                if (AutoCampaign)
                {
                    Timing.TimeStage("Auto-detecting Campaign", CampaignFromDromedLog);
                }

                var resources = new ResourceManager();
                Timing.TimeStage("Resource Path Gathering", () => resources.Initialise(context, CampaignName));

                var (loaded, mission) = Timing.TimeStage("Load Mission File", () =>
                {
                    var loaded = resources.TryGetDbFile(MissionName, out var mission);
                    return (loaded, mission);
                });

                if (!loaded || mission == null)
                {
                    return;
                }

                var lightMapper = new LightMapper(resources, mission);
                if (Inspect)
                {
                    lightMapper.Inspect();
                }
                else
                {
                    lightMapper.Light(SimpleVis);
                    if (resources.TryGetFilePath(MissionName, out var misPath))
                    {
                        var folder = Path.GetDirectoryName(misPath);
                        var misName = OutputName != null ? OutputName + Path.GetExtension(misPath) : MissionName;
                        var savePath = Path.Join(folder, misName);
                        Timing.TimeStage("Save Mission File", () => mission.Save(savePath));
                    }
                }
            });
            Timing.LogAll();
        }

        private void CampaignFromDromedLog()
        {
            try
            {
                Log.Information("Opening `DromEd.log`");

                var path = $"{InstallPath}/DromEd.log";
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (line == null || !line.StartsWith(": FM Path: "))
                    {
                        continue;
                    }

                    CampaignName = line[11..].Split(@"\").Last();
                    
                    // We don't early out here because we want to get the last occurence of an FM load
                    // If FMSel is configured to be returned to after closing DromEd then multiple FM loads can happen
                }

                if (CampaignName != null)
                {
                    Log.Information("Obtained campaign name: {CampaignName}", CampaignName);
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to automatically obtain campaign name.");
            }
        }
    }
    
    [CliCommand(Description = ".BIN model file handling")]
    public class ModelCommand
    {
        [CliCommand(Description = "Export models to .GLB")]
        public class ExportCommand
        {
            private readonly MaterialBuilder _defaultMaterial = MaterialBuilder.CreateDefault();

            [CliArgument(Description = "The path to the root Thief installation.")]
            public required string InstallPath { get; set; }

            [CliOption(Description = "The folder name of a fan mission.")]
            public string? CampaignName { get; set; } = null;

            [CliOption(Description = "The name of the model.")]
            public string? ModelName { get; set; } = null;

            [CliOption(
                Description = "Folder to output exported models to. If not set models will be exported alongside the original."
            )]
            public string? OutputDirectory { get; set; } = null;

            [CliOption(Description = "Disable terminal output.")]
            public bool Quiet { get; set; } = false;

            public void Run()
            {
                Program.ConfigureLogger(Quiet);

                var context = Timing.TimeStage("Initialise install context", () => new InstallContext(InstallPath));
                if (!context.Valid)
                {
                    Log.Error("Invalid install context");
                    return;
                }

                var resources = new ResourceManager();
                Timing.TimeStage("Resource Path Gathering", () => resources.Initialise(context, CampaignName));

                var modelCount = 0;
                if (ModelName != null)
                {
                    ExportModel(resources, ModelName);
                    modelCount++;
                }
                else
                {
                    foreach (var modelName in resources.ModelNames)
                    {
                        ExportModel(resources, modelName);
                        modelCount++;
                    }
                }

                Log.Information("Exported {Count} models.", modelCount);
            }

            private void ExportModel(ResourceManager resources, string modelName)
            {
                Log.Information("Exporting model: {Name}", modelName);
                if (!resources.TryGetModel(modelName, out var modelFile))
                {
                    Log.Error("Failed to load model: {Name}", modelName);
                    return;
                }

                var materials = BuildMaterialMap(resources, modelFile);

                var objCount = modelFile.Objects.Count;
                var meshes = new MeshBuilder<VertexPositionNormal, VertexTexture1>[objCount];
                var nodes = new NodeBuilder[objCount];
                for (var i = 0; i < objCount; i++)
                {
                    var subObject = modelFile.Objects[i];

                    var mesh = new MeshBuilder<VertexPositionNormal, VertexTexture1>(subObject.Name);
                    var matPolyMap = new Dictionary<int, List<int>>();

                    var polyCount = modelFile.Polygons.Count;
                    for (var j = 0; j < polyCount; j++)
                    {
                        var poly = modelFile.Polygons[j];

                        // Discards any polys that don't belong to this object
                        var startIdx = poly.VertexIndices[0].PositionIndex;
                        if (startIdx < subObject.VertexPositionStartIndex ||
                            startIdx >= subObject.VertexPositionStartIndex + subObject.VertexPositionCount)
                        {
                            continue;
                        }

                        if (matPolyMap.ContainsKey(poly.Data))
                        {
                            matPolyMap[poly.Data].Add(j);
                        }
                        else
                        {
                            matPolyMap[poly.Data] = [j];
                        }
                    }

                    foreach (var (materialIdx, polyIdxs) in matPolyMap)
                    {
                        var mat = materials.GetValueOrDefault(materialIdx, _defaultMaterial);
                        var prim = mesh.UsePrimitive(mat);
                        foreach (var polyIdx in polyIdxs)
                        {
                            var poly = modelFile.Polygons[polyIdx];
                            var faceNormal = modelFile.FaceNormals[poly.NormalIndex];
                            var vertexCount = poly.VertexIndices.Count;
                            var vertices = new Vector3[vertexCount];
                            var normals = new Vector3[vertexCount];
                            var uvs = new Vector2[vertexCount];
                            for (var j = 0; j < vertexCount; j++)
                            {
                                var vertexIndex = poly.VertexIndices[j];
                                vertices[j] = modelFile.VertexPositions[vertexIndex.PositionIndex];
                                normals[j] = poly.UseVertexNormals
                                    ? modelFile.VertexNormals[vertexIndex.NormalIndex].Normal
                                    : faceNormal;
                                uvs[j] = poly.Type == ModelPolygonType.Textured
                                    ? modelFile.VertexUvs[vertexIndex.UvIndex]
                                    : Vector2.Zero;
                            }

                            for (var j = 1; j < vertexCount - 1; j++)
                            {
                                prim.AddTriangle(
                                    new VERTEX(new VertexPositionNormal(vertices[0], normals[0]), uvs[0]),
                                    new VERTEX(new VertexPositionNormal(vertices[j + 1], normals[j + 1]), uvs[j + 1]),
                                    new VERTEX(new VertexPositionNormal(vertices[j], normals[j]), uvs[j])
                                );
                            }
                        }
                    }

                    var transform = subObject.JointType == ModelObjectType.Static  || subObject.JointIndex == -1
                        ? AffineTransform.Identity
                        : AffineTransform.CreateDecomposed(subObject.Transform);
                    var node = new NodeBuilder(subObject.Name);
                    node.SetLocalTransform(transform, false);

                    // Add vhots as empty nodes
                    for (var j = 0; j < subObject.VHotCount; j++)
                    {
                        var v = modelFile.VHots[subObject.VHotStartIndex + j];
                        var vhotNode = new NodeBuilder(v.Type.ToString());
                        vhotNode.SetLocalTransform(new AffineTransform(null, null, v.Position), false);
                        node.AddNode(vhotNode);
                    }

                    meshes[i] = mesh;
                    nodes[i] = node;
                }

                // Build node hierarchy
                for (var i = 0; i < objCount; i++)
                {
                    var subObject = modelFile.Objects[i];
                    var childIdx = subObject.ChildObjectIndex;
                    while (childIdx != -1)
                    {
                        // This can only happen if there's a loop in the relationship. This shouldn't ever be the case, but for
                        // some reason a few Thief 2 objects have this.
                        if (childIdx == i)
                        {
                            break;
                        }

                        nodes[i].AddNode(nodes[childIdx]);
                        childIdx = modelFile.Objects[childIdx].SiblingObjectIndex;
                    }
                }

                var scene = new SceneBuilder();
                for (var i = 0; i < objCount; i++)
                {
                    scene.AddRigidMesh(meshes[i], nodes[i]);
                }

                // GLTF uses different forward/right/up axes than Dark, but fortunately it's just a simple rotation
                scene.ApplyBasisTransform(Matrix4x4.CreateRotationX(float.DegreesToRadians(-90)));

                var exportName = Path.GetFileNameWithoutExtension(modelName);
                var exportDir = OutputDirectory ?? $"{AppDomain.CurrentDomain.BaseDirectory}/models/";
                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                scene.ToGltf2().SaveGLB($"{exportDir}/{exportName}.glb");
            }

            private Dictionary<int, MaterialBuilder> BuildMaterialMap(
                ResourceManager resources,
                ModelFile modelFile)
            {
                var materials = new Dictionary<int, MaterialBuilder>();
                foreach (var rawMaterial in modelFile.Materials)
                {
                    var slot = rawMaterial.Slot;

                    if (rawMaterial.Type == 0)
                    {
                        var resName = PathUtils.ConvertSeparator(Path.GetFileNameWithoutExtension(rawMaterial.Name));
                        if (!resources.TryGetObjectTextureVirtualPath(resName, out var virtualPath))
                        {
                            Log.Warning("Failed to find model texture, adding default material: {Name}, {Slot}",
                                resName, slot);
                            materials.Add(slot, _defaultMaterial);
                        }
                        else
                        {
                            if (TryLoadImage(resources, virtualPath, out var memoryImage))
                            {
                                var material = new MaterialBuilder(resName)
                                    .WithDoubleSide(false)
                                    .WithAlpha(AlphaMode.MASK)
                                    .WithBaseColor(ImageBuilder.From(memoryImage, resName));
                                Log.Debug("Adding texture material: {Name}, {Slot}", resName, slot);
                                materials.Add(slot, material);
                            }
                            else
                            {
                                Log.Debug("Unsupported model texture format, adding default material: {Name}, {Slot}",
                                    resName, slot);
                                materials.Add(slot, _defaultMaterial);
                            }
                        }
                    }
                    else
                    {
                        var r = rawMaterial.Color.R;
                        var g = rawMaterial.Color.G;
                        var b = rawMaterial.Color.B;
                        var colour = new Vector4(r, g, b, 255.0f) / 255.0f;
                        var material = new MaterialBuilder()
                            .WithDoubleSide(false)
                            .WithBaseColor(colour);
                        Log.Debug("Adding colour material: {Colour}, {Slot}", colour, slot);
                        materials.Add(slot, material);
                    }
                }

                return materials;
            }

            private static bool TryLoadImage(ResourceManager resources, string virtualPath, out MemoryImage memoryImage)
            {
                if (!resources.TryGetFileMemoryStream(virtualPath, out var stream))
                {
                    return false;
                }

                var ext = Path.GetExtension(virtualPath).ToLower();
                switch (ext)
                {
                    case ".png":
                    case ".dds":
                        memoryImage = new MemoryImage(stream.GetBuffer());
                        return true;
                    case ".gif":
                    {
                        using var image = new MagickImage(stream);
                        var colorZero = image.GetColormapColor(0);
                        if (colorZero != null)
                        {
                            image.Transparent(colorZero);
                        }

                        var pngStream = new MemoryStream();
                        image.Format = MagickFormat.Png;
                        image.Write(pngStream);
                        memoryImage = new MemoryImage(pngStream.GetBuffer());
                        return true;
                    }
                }

                Log.Warning("Cannot load texture at virtual path ({VPath}). Unsupported file type.", virtualPath);
                return false;
            }
        }
    }
}