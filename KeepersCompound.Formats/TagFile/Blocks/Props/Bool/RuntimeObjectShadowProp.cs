namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class RuntimeObjectShadowProp : BoolProp, IFromType<RuntimeObjectShadowProp, BoolProp>
{
    public static RuntimeObjectShadowProp FromType(BoolProp p)
    {
        return new RuntimeObjectShadowProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}