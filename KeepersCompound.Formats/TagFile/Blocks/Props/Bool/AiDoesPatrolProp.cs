namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiDoesPatrolProp : BoolProp, IFromType<AiDoesPatrolProp, BoolProp>
{
    public static AiDoesPatrolProp FromType(BoolProp p)
    {
        return new AiDoesPatrolProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}