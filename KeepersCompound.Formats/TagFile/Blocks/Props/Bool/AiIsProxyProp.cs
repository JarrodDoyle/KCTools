namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiIsProxyProp : BoolProp, IFromType<AiIsProxyProp, BoolProp>
{
    public static AiIsProxyProp FromType(BoolProp p)
    {
        return new AiIsProxyProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}