namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class DisableHeadTrackingProp : BoolProp, IFromType<DisableHeadTrackingProp, BoolProp>
{
    public static DisableHeadTrackingProp FromType(BoolProp p)
    {
        return new DisableHeadTrackingProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}