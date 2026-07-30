namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiRespondToDamageProp : BoolProp, IFromType<AiRespondToDamageProp, BoolProp>
{
    public static AiRespondToDamageProp FromType(BoolProp p)
    {
        return new AiRespondToDamageProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}