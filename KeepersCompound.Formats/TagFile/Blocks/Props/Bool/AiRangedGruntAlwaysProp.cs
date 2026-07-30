namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiRangedGruntAlwaysProp : BoolProp, IFromType<AiRangedGruntAlwaysProp, BoolProp>
{
    public static AiRangedGruntAlwaysProp FromType(BoolProp p)
    {
        return new AiRangedGruntAlwaysProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}