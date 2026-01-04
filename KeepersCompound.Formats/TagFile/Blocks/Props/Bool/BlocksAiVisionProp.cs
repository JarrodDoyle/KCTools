namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BlocksAiVisionProp : BoolProp, IFromType<BlocksAiVisionProp, BoolProp>
{
    public static BlocksAiVisionProp FromType(BoolProp p)
    {
        return new BlocksAiVisionProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}