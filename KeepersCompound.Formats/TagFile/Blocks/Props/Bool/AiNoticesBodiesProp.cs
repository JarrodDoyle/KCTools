namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiNoticesBodiesProp : BoolProp, IFromType<AiNoticesBodiesProp, BoolProp>
{
    public static AiNoticesBodiesProp FromType(BoolProp p)
    {
        return new AiNoticesBodiesProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}