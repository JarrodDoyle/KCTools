namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class FaceMotionsProp : BoolProp, IFromType<FaceMotionsProp, BoolProp>
{
    public static FaceMotionsProp FromType(BoolProp p)
    {
        return new FaceMotionsProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}