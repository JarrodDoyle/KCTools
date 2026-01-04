namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class InvNoDropProp : BoolProp, IFromType<InvNoDropProp, BoolProp>
{
    public static InvNoDropProp FromType(BoolProp p)
    {
        return new InvNoDropProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}