namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class TransientProp : BoolProp, IFromType<TransientProp, BoolProp>
{
    public static TransientProp FromType(BoolProp p)
    {
        return new TransientProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}