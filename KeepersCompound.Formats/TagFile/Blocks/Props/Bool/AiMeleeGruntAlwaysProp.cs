namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiMeleeGruntAlwaysProp : BoolProp, IFromType<AiMeleeGruntAlwaysProp, BoolProp>
{
    public static AiMeleeGruntAlwaysProp FromType(BoolProp p)
    {
        return new AiMeleeGruntAlwaysProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}