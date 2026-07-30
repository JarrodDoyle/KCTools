namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class ItemStoreProp : BoolProp, IFromType<ItemStoreProp, BoolProp>
{
    public static ItemStoreProp FromType(BoolProp p)
    {
        return new ItemStoreProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}