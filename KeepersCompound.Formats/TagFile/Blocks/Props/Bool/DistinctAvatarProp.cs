namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class DistinctAvatarProp : BoolProp, IFromType<DistinctAvatarProp, BoolProp>
{
    public static DistinctAvatarProp FromType(BoolProp p)
    {
        return new DistinctAvatarProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}