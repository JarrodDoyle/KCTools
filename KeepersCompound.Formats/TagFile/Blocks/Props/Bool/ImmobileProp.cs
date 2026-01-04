namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class ImmobileProp : BoolProp, IFromType<ImmobileProp, BoolProp>
{
    public static ImmobileProp FromType(BoolProp p)
    {
        return new ImmobileProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}