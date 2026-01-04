namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiRandomPatrolProp : BoolProp, IFromType<AiRandomPatrolProp, BoolProp>
{
    public static AiRandomPatrolProp FromType(BoolProp p)
    {
        return new AiRandomPatrolProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}