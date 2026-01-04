namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiReturnOriginProp : BoolProp, IFromType<AiReturnOriginProp, BoolProp>
{
    public static AiReturnOriginProp FromType(BoolProp p)
    {
        return new AiReturnOriginProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}