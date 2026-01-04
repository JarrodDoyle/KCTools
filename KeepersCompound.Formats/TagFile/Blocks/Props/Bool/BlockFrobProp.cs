namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BlockFrobProp : BoolProp, IFromType<BlockFrobProp, BoolProp>
{
    public static BlockFrobProp FromType(BoolProp p)
    {
        return new BlockFrobProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}