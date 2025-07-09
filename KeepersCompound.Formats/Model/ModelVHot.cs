using System.Numerics;

namespace KeepersCompound.Formats.Model;

public class ModelVHot
{
    /// <summary>
    /// Which type of VHot is it? VHot types on a model must be unique.
    /// </summary>
    public required ModelVHotType Type { get; set; }

    /// <summary>
    /// Position in global space. When <see cref="Type"/> is <see cref="ModelVHotType.LightDirection"/> the direction is <see cref="Position"/> relative to the <see cref="ModelVHotType.LightPosition"/> VHot.
    /// </summary>
    public required Vector3 Position { get; set; }
}