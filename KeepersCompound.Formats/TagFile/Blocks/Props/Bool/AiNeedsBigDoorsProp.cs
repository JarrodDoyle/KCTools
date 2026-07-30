namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiNeedsBigDoorsProp : BoolProp, IFromType<AiNeedsBigDoorsProp, BoolProp>
{
    public static AiNeedsBigDoorsProp FromType(BoolProp p)
    {
        return new AiNeedsBigDoorsProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}