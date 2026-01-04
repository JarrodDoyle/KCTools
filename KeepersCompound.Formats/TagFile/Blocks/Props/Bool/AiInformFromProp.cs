namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiInformFromProp : BoolProp, IFromType<AiInformFromProp, BoolProp>
{
    public static AiInformFromProp FromType(BoolProp p)
    {
        return new AiInformFromProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}