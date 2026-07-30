namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiNoMultiplayerHandoffProp : BoolProp, IFromType<AiNoMultiplayerHandoffProp, BoolProp>
{
    public static AiNoMultiplayerHandoffProp FromType(BoolProp p)
    {
        return new AiNoMultiplayerHandoffProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}