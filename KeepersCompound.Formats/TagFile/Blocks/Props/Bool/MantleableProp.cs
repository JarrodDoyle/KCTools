namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class MantleableProp : BoolProp, IFromType<MantleableProp, BoolProp>
{
    public static MantleableProp FromType(BoolProp p)
    {
        return new MantleableProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}