using System.Numerics;

namespace KeepersCompound.Portalisation;

public class BspNode
{
    public bool Leaf => LeftChild == null && RightChild == null;
    public CsgMedia Medium { get; set; }
    public Plane SplitPlane { get; set; }

    public BspNode? Parent { get; }
    public BspNode? LeftChild { get; set; }
    public BspNode? RightChild { get; set; }

    public int CellId = -1;
    internal SortedSet<int> InsertedBrushIds { get; } = [];

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
}