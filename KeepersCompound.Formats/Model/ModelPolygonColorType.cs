namespace KeepersCompound.Formats.Model;

public enum ModelPolygonColorType
{
    /// <summary>
    /// Only valid value when <see cref="ModelPolygonType.Textured"/>
    /// </summary>
    None,
    
    /// <summary>
    /// Polygon color indexes engine palette by <see cref="ModelPolygon.Data"/>.
    /// </summary>
    Paletted,
    
    /// <summary>
    /// Polygon color uses coloured material with <see cref="ModelMaterial.Slot"/> equal to <see cref="ModelPolygon.Data"/>.
    /// </summary>
    Coloured,
}