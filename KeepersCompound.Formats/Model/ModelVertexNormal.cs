using System.Numerics;

namespace KeepersCompound.Formats.Model;

public class ModelVertexNormal
{
    public required ushort MaterialId { get; set; }
    public required ushort VertexId { get; set; }
    public required Vector3 Normal { get; set; }
}