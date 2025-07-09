using System.Numerics;

namespace KeepersCompound.Formats.Model;

/// <summary>
/// Structure representing a Dark Engine .BIN model file. Read and write using a <see cref="ModelFileParser"/>.
/// </summary>
public class ModelFile
{
    /// <summary>
    /// Version of BSP used to generate model. Supported versions are 3 and 4.
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// Maximum 8 characters. Additional characters will be truncated on write.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Model bounding sphere radius centered on (0, 0, 0).
    /// </summary>
    public required float Radius { get; set; }

    /// <summary>
    /// Maximum bounding sphere radius of the models polygons.
    /// </summary>
    public required float MaxPolygonRadius { get; set; }

    /// <summary>
    /// Minimum point of the models AABB
    /// </summary>
    public required Vector3 MinBounds { get; set; }

    /// <summary>
    /// Maximum point of the models AABB
    /// </summary>
    public required Vector3 MaxBounds { get; set; }

    /// <summary>
    /// Model center offset
    /// </summary>
    public required Vector3 Center { get; set; }

    /// <summary>
    /// Uses transparency on one or more materials. Only supported on version 4.
    /// </summary>
    public required bool Transparency { get; set; }

    /// <summary>
    /// Uses self-illumination on one or more materials. Only supported on version 4.
    /// </summary>
    public required bool SelfIllumination { get; set; }

    /// <summary>
    /// Number of joints/parameters. DromEd can only interact with the first 6.
    /// </summary>
    public required ushort JointCount { get; set; }

    /// <summary>
    /// Number of VCalls in the BSP tree
    /// </summary>
    public required byte VCallCount { get; set; }

    /// <summary>
    /// Global vertex position list
    /// </summary>
    public required List<Vector3> VertexPositions { get; set; }

    /// <summary>
    /// Global vertex UV list
    /// </summary>
    public required List<Vector2> VertexUvs { get; set; }

    /// <summary>
    /// Global vertex normal list
    /// </summary>
    public required List<ModelVertexNormal> VertexNormals { get; set; }

    /// <summary>
    /// Global face normal list
    /// </summary>
    public required List<Vector3> FaceNormals { get; set; }

    /// <summary>
    /// Global polygon list
    /// </summary>
    public required List<ModelPolygon> Polygons { get; set; }

    /// <summary>
    /// Global vhot list. Each vhot must have a unique <see cref="ModelVHotType"/>.
    /// </summary>
    public required List<ModelVHot> VHots { get; set; }

    public required List<ModelMaterial> Materials { get; set; }

    public required List<ModelObject> Objects { get; set; }

    /// <summary>
    /// Unknown structure at this point.
    /// </summary>
    public required List<byte> BspNodeData { get; set; }
}