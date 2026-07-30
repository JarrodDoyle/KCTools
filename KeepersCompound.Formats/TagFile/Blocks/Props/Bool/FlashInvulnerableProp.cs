namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class FlashInvulnerableProp : BoolProp, IFromType<FlashInvulnerableProp, BoolProp>
{
    public static FlashInvulnerableProp FromType(BoolProp p)
    {
        return new FlashInvulnerableProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}