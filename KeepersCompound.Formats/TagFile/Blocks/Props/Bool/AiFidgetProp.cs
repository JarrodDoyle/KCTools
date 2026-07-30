namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiFidgetProp : BoolProp, IFromType<AiFidgetProp, BoolProp>
{
    public static AiFidgetProp FromType(BoolProp p)
    {
        return new AiFidgetProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}