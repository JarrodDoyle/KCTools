namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class LocalCopyProp : BoolProp, IFromType<LocalCopyProp, BoolProp>
{
    public static LocalCopyProp FromType(BoolProp p)
    {
        return new LocalCopyProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}