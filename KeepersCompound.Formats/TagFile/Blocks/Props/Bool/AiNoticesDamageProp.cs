namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiNoticesDamageProp : BoolProp, IFromType<AiNoticesDamageProp, BoolProp>
{
    public static AiNoticesDamageProp FromType(BoolProp p)
    {
        return new AiNoticesDamageProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}