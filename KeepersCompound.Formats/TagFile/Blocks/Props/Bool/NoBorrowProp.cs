namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class NoBorrowProp : BoolProp, IFromType<NoBorrowProp, BoolProp>
{
    public static NoBorrowProp FromType(BoolProp p)
    {
        return new NoBorrowProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}