using System.Numerics;

namespace KeepersCompound.Formats.TagFile.Blocks.Props.Position;

public class PositionProp : AbstractProp
{
    public required Vector3 Location { get; set; }
    public required int CellHint { get; set; }
    public required Vector3 Rotation { get; set; }
}