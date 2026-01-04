namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class DoorStaticLightProp : BoolProp, IFromType<DoorStaticLightProp, BoolProp>
{
    public static DoorStaticLightProp FromType(BoolProp p)
    {
        return new DoorStaticLightProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}