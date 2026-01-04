namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiSaveConversationProp : BoolProp, IFromType<AiSaveConversationProp, BoolProp>
{
    public static AiSaveConversationProp FromType(BoolProp p)
    {
        return new AiSaveConversationProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}