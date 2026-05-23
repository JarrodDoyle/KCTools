using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;

namespace KeepersCompound.Portalisation;

public class WrTreeBuilder
{
    private List<Plane> Planes { get; }
    private List<WorldRep.BspTree.Node> Nodes { get; }

    public WrTreeBuilder()
    {
        Planes = [];
        Nodes = [];
    }

    public void AddCsgTree(PlaneManager planeManager, BspNode csgRoot)
    {
        AddCsgTreeInternal(planeManager, csgRoot, 0x00FFFFFF, -1);
    }

    public void AddSplit(Plane plane, int fromCell, int toCell)
    {
        var nodeCount = Nodes.Count;
        for (var i = 0; i < nodeCount; i++)
        {
            var node = Nodes[i];
            var flags = (node.ParentIndex >> 24) & 0xFF;
            if ((flags & 0x01) == 0 || node.InsideIndex != fromCell)
            {
                continue;
            }

            // TODO: How to check if needs reverse flag?
            node.ParentIndex ^= 0x01 << 24;
            node.PlaneId = Planes.Count;
            node.InsideIndex = nodeCount;
            node.OutsideIndex = nodeCount + 1;
            Planes.Add(plane);
            Nodes.Add(new WorldRep.BspTree.Node
            {
                ParentIndex = i | 0x01 << 24,
                CellId = -1,
                PlaneId = -1,
                InsideIndex = fromCell,
                OutsideIndex = 0,
            });
            Nodes.Add(new WorldRep.BspTree.Node
            {
                ParentIndex = i | 0x01 << 24,
                CellId = -1,
                PlaneId = -1,
                InsideIndex = toCell,
                OutsideIndex = 0,
            });
            Nodes[i] = node;
            break;
        }
    }

    public WorldRep.BspTree ToWrTree()
    {
        return new WorldRep.BspTree
        {
            PlaneCount = (uint)Planes.Count,
            NodeCount = (uint)Nodes.Count,
            Planes = Planes.ToArray(),
            Nodes = Nodes.ToArray()
        };
    }

    private void AddCsgTreeInternal(
        PlaneManager planeManager,
        BspNode tree,
        int parentIndex,
        int cellId)
    {
        // According to vfig (see vfig/misdeed) Marked flag (0x2) gets cleared on load so I can just ignore it
        var node = new WorldRep.BspTree.Node
        {
            ParentIndex = parentIndex,
            CellId = -1,
            PlaneId = -1,
            InsideIndex = 0x00FFFFFF,
            OutsideIndex = 0x00FFFFFF,
        };

        cellId = tree.CellId == -1 ? cellId : tree.CellId;
        if (tree.Leaf || tree.CellId != -1)
        {
            node.ParentIndex |= 0x01 << 24;
            node.InsideIndex = cellId;
            node.OutsideIndex = 0; // DromEd writes garbage here. It's just padding and means nothing
            Nodes.Add(node);
            return;
        }

        node.PlaneId = Planes.Count;
        Planes.Add(planeManager.GetPlane(tree.SplitPlane));
        Nodes.Add(node);
        parentIndex = Nodes.Count - 1;
        if (tree.LeftChild != null && tree.LeftChild.Medium != CsgMedia.Solid)
        {
            node.InsideIndex = Nodes.Count;
            AddCsgTreeInternal(planeManager, tree.LeftChild, parentIndex, cellId);
        }

        if (tree.RightChild != null && tree.RightChild.Medium != CsgMedia.Solid)
        {
            node.OutsideIndex = Nodes.Count;
            AddCsgTreeInternal(planeManager, tree.RightChild, parentIndex, cellId);
        }

        if (node.OutsideIndex != 0x00FFFFFF && node.InsideIndex == 0x00FFFFFF)
        {
            node.InsideIndex = node.OutsideIndex;
            node.OutsideIndex = 0x00FFFFFF;
            node.ParentIndex |= 0x4 << 24;
        }

        Nodes[parentIndex] = node;
    }
}