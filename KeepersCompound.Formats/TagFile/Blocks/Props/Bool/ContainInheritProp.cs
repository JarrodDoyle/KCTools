namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class ContainInheritProp : BoolProp, IFromType<ContainInheritProp, BoolProp>
{
    public static ContainInheritProp FromType(BoolProp p)
    {
        return new ContainInheritProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}