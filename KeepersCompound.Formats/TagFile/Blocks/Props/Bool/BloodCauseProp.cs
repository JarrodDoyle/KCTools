namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BloodCauseProp : BoolProp, IFromType<BloodCauseProp, BoolProp>
{
    public static BloodCauseProp FromType(BoolProp p)
    {
        return new BloodCauseProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}