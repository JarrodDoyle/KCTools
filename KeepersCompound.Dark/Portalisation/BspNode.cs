using System.Numerics;
using KeepersCompound.Dark.Portalisation.Brush;

namespace KeepersCompound.Dark.Portalisation;

public class BspNode
{
    public bool Leaf => LeftChild == null && RightChild == null;
    public CsgMedia Medium { get; set; }
    public Plane SplitPlane { get; set; }

    public BspNode? Parent { get; }
    public BspNode? LeftChild { get; set; }
    public BspNode? RightChild { get; set; }

    public List<BrushDef> ContainedBrushes { get; } = [];
    public List<BspPoly> Polys { get; } = [];
    public int CellId = -1;

    public BspNode(BspNode? parent)
    {
        Parent = parent;
        Medium = CsgMedia.None;
    }

    public void Traverse(Action<BspNode> action)
    {
        action(this);
        LeftChild?.Traverse(action);
        RightChild?.Traverse(action);
    }

    public int EncodeMedium()
    {
        return EncodeMediumInternal(new(Comparer<BrushDef>.Create((a, b) => a.Time.CompareTo(b.Time))));
    }

    private int EncodeMediumInternal(SortedSet<BrushDef> active)
    {
        var surfaceCount = 0;
        foreach (var brush in ContainedBrushes)
        {
            active.Add(brush);
        }

        if (Leaf)
        {
            Medium = active.Aggregate(CsgMedia.Solid, (m, b) => CsgMediaTable.GetMedium(b.Operation, m));
        }
        else
        {
            surfaceCount += LeftChild!.EncodeMediumInternal(active);
            surfaceCount += RightChild!.EncodeMediumInternal(active);
            if (LeftChild.Medium == RightChild.Medium)
            {
                Medium = LeftChild.Medium;
            }
            else
            {
                if (LeftChild.Medium != CsgMedia.None) surfaceCount++;
                if (RightChild.Medium != CsgMedia.None) surfaceCount++;
            }
        }

        foreach (var brush in ContainedBrushes)
        {
            active.Remove(brush);
        }

        return surfaceCount;
    }
}