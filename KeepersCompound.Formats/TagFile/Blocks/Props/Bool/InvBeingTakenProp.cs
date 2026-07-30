namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class InvBeingTakenProp : BoolProp, IFromType<InvBeingTakenProp, BoolProp>
{
    public static InvBeingTakenProp FromType(BoolProp p)
    {
        return new InvBeingTakenProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}