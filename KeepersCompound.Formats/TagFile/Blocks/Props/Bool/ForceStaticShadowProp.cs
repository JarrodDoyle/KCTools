namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class ForceStaticShadowProp : BoolProp, IFromType<ForceStaticShadowProp, BoolProp>
{
    public static ForceStaticShadowProp FromType(BoolProp p)
    {
        return new ForceStaticShadowProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}