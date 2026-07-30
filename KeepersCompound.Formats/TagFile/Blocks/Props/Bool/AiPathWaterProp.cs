namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiPathWaterProp : BoolProp, IFromType<AiPathWaterProp, BoolProp>
{
    public static AiPathWaterProp FromType(BoolProp p)
    {
        return new AiPathWaterProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}