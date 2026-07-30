namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiIgnoresCamerasProp : BoolProp, IFromType<AiIgnoresCamerasProp, BoolProp>
{
    public static AiIgnoresCamerasProp FromType(BoolProp p)
    {
        return new AiIgnoresCamerasProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}