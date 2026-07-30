namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class FacesVelocityProp : BoolProp, IFromType<FacesVelocityProp, BoolProp>
{
    public static FacesVelocityProp FromType(BoolProp p)
    {
        return new FacesVelocityProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}