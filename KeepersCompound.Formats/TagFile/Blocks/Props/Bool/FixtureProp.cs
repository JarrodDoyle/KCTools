namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class FixtureProp : BoolProp, IFromType<FixtureProp, BoolProp>
{
    public static FixtureProp FromType(BoolProp p)
    {
        return new FixtureProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}