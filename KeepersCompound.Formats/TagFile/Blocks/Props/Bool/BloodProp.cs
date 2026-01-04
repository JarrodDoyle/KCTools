namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BloodProp : BoolProp, IFromType<BloodProp, BoolProp>
{
    public static BloodProp FromType(BoolProp p)
    {
        return new BloodProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}