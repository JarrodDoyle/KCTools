namespace KeepersCompound.Dark.Portalisation.Brush;

internal class InsertionBrush
{
    public int Time { get; }
    public BrushOperation Operation { get; }
    public List<TreeInsertionPoly> Faces { get; }

    public InsertionBrush(int time, BrushOperation operation, List<TreeInsertionPoly> faces)
    {
        Time = time;
        Operation = operation;
        Faces = faces;
    }
}