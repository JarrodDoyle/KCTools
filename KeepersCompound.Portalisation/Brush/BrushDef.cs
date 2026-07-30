namespace KeepersCompound.Portalisation.Brush;

public class BrushDef
{
    public BrushOperation Operation { get; }
    public List<BrushDefFace> Faces { get; }

    public BrushDef(BrushOperation operation, List<BrushDefFace> faces)
    {
        Operation = operation;
        Faces = faces;
    }
}