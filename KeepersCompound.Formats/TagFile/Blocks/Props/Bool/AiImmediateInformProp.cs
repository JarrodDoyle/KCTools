namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiImmediateInformProp : BoolProp, IFromType<AiImmediateInformProp, BoolProp>
{
    public static AiImmediateInformProp FromType(BoolProp p)
    {
        return new AiImmediateInformProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}