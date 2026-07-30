namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiFleeAwareProp : BoolProp, IFromType<AiFleeAwareProp, BoolProp>
{
    public static AiFleeAwareProp FromType(BoolProp p)
    {
        return new AiFleeAwareProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}