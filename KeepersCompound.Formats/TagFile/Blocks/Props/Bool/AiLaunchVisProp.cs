namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiLaunchVisProp : BoolProp, IFromType<AiLaunchVisProp, BoolProp>
{
    public static AiLaunchVisProp FromType(BoolProp p)
    {
        return new AiLaunchVisProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}