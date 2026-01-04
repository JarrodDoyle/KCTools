namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiPathableObjectProp : BoolProp, IFromType<AiPathableObjectProp, BoolProp>
{
    public static AiPathableObjectProp FromType(BoolProp p)
    {
        return new AiPathableObjectProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}