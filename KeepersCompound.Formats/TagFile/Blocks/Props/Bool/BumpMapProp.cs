namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BumpMapProp : BoolProp, IFromType<BumpMapProp, BoolProp>
{
    public static BumpMapProp FromType(BoolProp p)
    {
        return new BumpMapProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}