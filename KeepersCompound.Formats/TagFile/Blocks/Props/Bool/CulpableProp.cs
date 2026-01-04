namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class CulpableProp : BoolProp, IFromType<CulpableProp, BoolProp>
{
    public static CulpableProp FromType(BoolProp p)
    {
        return new CulpableProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}