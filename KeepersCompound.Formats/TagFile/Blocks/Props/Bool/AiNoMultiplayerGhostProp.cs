namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiNoMultiplayerGhostProp : BoolProp, IFromType<AiNoMultiplayerGhostProp, BoolProp>
{
    public static AiNoMultiplayerGhostProp FromType(BoolProp p)
    {
        return new AiNoMultiplayerGhostProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}