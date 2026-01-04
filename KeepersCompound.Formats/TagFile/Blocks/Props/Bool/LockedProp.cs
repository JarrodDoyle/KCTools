namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class LockedProp : BoolProp, IFromType<LockedProp, BoolProp>
{
    public static LockedProp FromType(BoolProp p)
    {
        return new LockedProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}