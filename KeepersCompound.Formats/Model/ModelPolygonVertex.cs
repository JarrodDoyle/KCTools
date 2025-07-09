namespace KeepersCompound.Formats.Model;

public class ModelPolygonVertex
{
    /// <summary>
    /// Index into <see cref="ModelFile.VertexPositions"/>.
    /// </summary>
    public required ushort PositionIndex { get; set; }

    /// <summary>
    /// Index into <see cref="ModelFile.VertexNormals"/>.
    /// </summary>
    public required ushort NormalIndex { get; set; }

    /// <summary>
    /// Index into <see cref="ModelFile.VertexUvs"/>. Only used for textured polygons.
    /// </summary>
    public required ushort UvIndex { get; set; }
}