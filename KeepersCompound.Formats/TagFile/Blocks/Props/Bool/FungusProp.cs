namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class FungusProp : BoolProp, IFromType<FungusProp, BoolProp>
{
    public static FungusProp FromType(BoolProp p)
    {
        return new FungusProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}