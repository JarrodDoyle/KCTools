namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiUsesDoorsProp : BoolProp, IFromType<AiUsesDoorsProp, BoolProp>
{
    public static AiUsesDoorsProp FromType(BoolProp p)
    {
        return new AiUsesDoorsProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}