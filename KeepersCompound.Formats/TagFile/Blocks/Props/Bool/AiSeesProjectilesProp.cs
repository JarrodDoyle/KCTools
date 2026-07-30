namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class AiSeesProjectilesProp : BoolProp, IFromType<AiSeesProjectilesProp, BoolProp>
{
    public static AiSeesProjectilesProp FromType(BoolProp p)
    {
        return new AiSeesProjectilesProp
        {
            ObjectId = p.ObjectId,
            Length = p.Length,
            Value = p.Value,
        };
    }
}