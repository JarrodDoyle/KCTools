namespace KeepersCompound.Formats.Model;

public class ModelPolygon
{
    /// <summary>
    /// Usually equal to the position of the poly in <see cref="ModelFile.Polygons"/>.
    /// </summary>
    public required ushort Index { get; set; }

    /// <summary>
    /// What type of polygon are we? Affects how <see cref="Data"/> is used.
    /// </summary>
    public required ModelPolygonType Type { get; set; }
    public required ModelPolygonColorType ColorType { get; set; }

    /// <summary>
    /// Used as either an engine palette index or a material <see cref="ModelMaterial.Slot"/> depending on <see cref="Type"/>.
    /// </summary>
    public required ushort Data { get; set; }

    /// <summary>
    /// Should the vertex normals be used or ignored?
    /// </summary>
    public required bool UseVertexNormals { get; set; }

    /// <summary>
    /// Index into <see cref="ModelFile.FaceNormals"/>.
    /// </summary>
    public required ushort NormalIndex { get; set; }

    /// <summary>
    /// Distance of normal along <see cref="NormalIndex"/>.
    /// </summary>
    public required float NormalDistance { get; set; }

    /// <summary>
    /// Indices into the appropriate global model vertex data lists.
    /// </summary>
    public required List<ModelPolygonVertex> VertexIndices { get; set; }

    /// <summary>
    /// ID of the material used by this polygon. Only used in <see cref="ModelFile.Version"/> 4.
    /// </summary>
    public required byte MaterialId { get; set; }
}