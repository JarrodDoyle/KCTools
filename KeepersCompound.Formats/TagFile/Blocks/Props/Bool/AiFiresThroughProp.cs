namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiFiresThroughProp : BoolProp, IFromType<AiFiresThroughProp, BoolProp>
{
    public static AiFiresThroughProp FromType(BoolProp p)
    {
        return new AiFiresThroughProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}