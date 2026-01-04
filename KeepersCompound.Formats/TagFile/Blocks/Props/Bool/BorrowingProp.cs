namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BorrowingProp : BoolProp, IFromType<BorrowingProp, BoolProp>
{
    public static BorrowingProp FromType(BoolProp p)
    {
        return new BorrowingProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}