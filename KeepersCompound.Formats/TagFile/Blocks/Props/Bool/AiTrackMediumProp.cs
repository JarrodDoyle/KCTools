namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiTrackMediumProp : BoolProp, IFromType<AiTrackMediumProp, BoolProp>
{
    public static AiTrackMediumProp FromType(BoolProp p)
    {
        return new AiTrackMediumProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}