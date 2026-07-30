namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiIsSmallProp : BoolProp, IFromType<AiIsSmallProp, BoolProp>
{
    public static AiIsSmallProp FromType(BoolProp p)
    {
        return new AiIsSmallProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}