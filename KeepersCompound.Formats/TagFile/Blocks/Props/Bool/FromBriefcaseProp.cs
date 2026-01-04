namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class FromBriefcaseProp : BoolProp, IFromType<FromBriefcaseProp, BoolProp>
{
    public static FromBriefcaseProp FromType(BoolProp p)
    {
        return new FromBriefcaseProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}