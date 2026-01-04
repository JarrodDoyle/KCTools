namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiPathExactObbProp : BoolProp, IFromType<AiPathExactObbProp, BoolProp>
{
    public static AiPathExactObbProp FromType(BoolProp p)
    {
        return new AiPathExactObbProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}