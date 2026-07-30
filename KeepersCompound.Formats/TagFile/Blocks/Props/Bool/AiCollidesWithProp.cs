namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiCollidesWithProp : BoolProp, IFromType<AiCollidesWithProp, BoolProp>
{
    public static AiCollidesWithProp FromType(BoolProp p)
    {
        return new AiCollidesWithProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}