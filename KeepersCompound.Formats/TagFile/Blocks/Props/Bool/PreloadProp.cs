namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class PreloadProp : BoolProp, IFromType<PreloadProp, BoolProp>
{
    public static PreloadProp FromType(BoolProp p)
    {
        return new PreloadProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}