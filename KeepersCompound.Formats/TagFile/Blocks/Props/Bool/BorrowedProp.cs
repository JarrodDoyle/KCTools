namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BorrowedProp : BoolProp, IFromType<BorrowedProp, BoolProp>
{
    public static BorrowedProp FromType(BoolProp p)
    {
        return new BorrowedProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}