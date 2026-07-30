namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class NonPhysCreatureProp : BoolProp, IFromType<NonPhysCreatureProp, BoolProp>
{
    public static NonPhysCreatureProp FromType(BoolProp p)
    {
        return new NonPhysCreatureProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}