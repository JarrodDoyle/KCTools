namespace KeepersCompound.Formats.Model;

public enum ModelPolygonType
{
    /// <summary>
    /// Polygon is unrendered. I've never seen this, but it's theoretically possible.
    /// </summary>
    None,

    /// <summary>
    /// Polygon is rendered as a ngon wireframe. Should be used in conjunction with <see cref="ModelPolygonColorType.Paletted"/> or <see cref="ModelPolygonColorType. Coloured"/>.
    /// </summary>
    Wireframe,

    /// <summary>
    /// Polygon is rendered as a solid color. Should be used in conjunction with <see cref="ModelPolygonColorType.Paletted"/> or <see cref="ModelPolygonColorType. Coloured"/>.
    /// </summary>
    Solid,
    
    /// <summary>
    /// Polygon is rendered with a texture using a texture material with <see cref="ModelMaterial.Slot"/> equal to <see cref="ModelPolygon.Data"/>.
    /// </summary>
    Textured,
}