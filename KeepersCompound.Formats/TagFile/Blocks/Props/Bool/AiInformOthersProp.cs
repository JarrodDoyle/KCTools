namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiInformOthersProp : BoolProp, IFromType<AiInformOthersProp, BoolProp>
{
    public static AiInformOthersProp FromType(BoolProp p)
    {
        return new AiInformOthersProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}