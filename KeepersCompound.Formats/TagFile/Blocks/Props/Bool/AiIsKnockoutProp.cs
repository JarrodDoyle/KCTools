namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiIsKnockoutProp : BoolProp, IFromType<AiIsKnockoutProp, BoolProp>
{
    public static AiIsKnockoutProp FromType(BoolProp p)
    {
        return new AiIsKnockoutProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}