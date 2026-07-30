namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class HasRefsProp : BoolProp, IFromType<HasRefsProp, BoolProp>
{
    public static HasRefsProp FromType(BoolProp p)
    {
        return new HasRefsProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}