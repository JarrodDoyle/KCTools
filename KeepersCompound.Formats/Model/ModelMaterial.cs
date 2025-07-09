using System.Drawing;
using System.Numerics;

namespace KeepersCompound.Formats.Model;

public class ModelMaterial
{
    /// <summary>
    /// Maximum 16 characters. Filename including extension when textured.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Material type.
    /// </summary>
    public required ModelMaterialType Type { get; set; }

    /// <summary>
    /// Unique ID of the material.
    /// </summary>
    public required byte Slot { get; set; }

    /// <summary>
    /// How transparent is this material? Range 0-1. Only applies on <see cref="ModelFile.Version"/> 4.
    /// </summary>
    public required float Transparency { get; set; }

    /// <summary>
    /// How self-illuminating is this material? Range 0-1. Only applies on <see cref="ModelFile.Version"/> 4.
    /// </summary>
    public required float SelfIllumination { get; set; }

    /// <summary>
    /// Previous format specs online have this as MaxTexelSize but the values seem like garbage to me.
    /// </summary>
    public required Vector2 MaxTexelSize { get; set; }

    /// <summary>
    /// ARGB colour. Only relevant when <see cref="Type"/> is <see cref="ModelMaterialType.Color"/>.
    /// </summary>
    public required Color Color { get; set; }

    /// <summary>
    /// Index into the global engine palette.  Only relevant when <see cref="Type"/> is <see cref="ModelMaterialType.Color"/>.
    /// </summary>
    public required uint PaletteIndex { get; set; }
}