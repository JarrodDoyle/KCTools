namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiOnlyNoticesPlayerProp : BoolProp, IFromType<AiOnlyNoticesPlayerProp, BoolProp>
{
    public static AiOnlyNoticesPlayerProp FromType(BoolProp p)
    {
        return new AiOnlyNoticesPlayerProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}