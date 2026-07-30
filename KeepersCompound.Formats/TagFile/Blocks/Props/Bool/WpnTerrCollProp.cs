namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class WpnTerrCollProp : BoolProp, IFromType<WpnTerrCollProp, BoolProp>
{
    public static WpnTerrCollProp FromType(BoolProp p)
    {
        return new WpnTerrCollProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}