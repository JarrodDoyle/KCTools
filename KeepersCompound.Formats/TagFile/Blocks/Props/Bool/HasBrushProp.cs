namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class HasBrushProp : BoolProp, IFromType<HasBrushProp, BoolProp>
{
    public static HasBrushProp FromType(BoolProp p)
    {
        return new HasBrushProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}