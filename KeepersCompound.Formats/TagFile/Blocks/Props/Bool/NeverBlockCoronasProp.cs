namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class NeverBlockCoronasProp : BoolProp, IFromType<NeverBlockCoronasProp, BoolProp>
{
    public static NeverBlockCoronasProp FromType(BoolProp p)
    {
        return new NeverBlockCoronasProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}