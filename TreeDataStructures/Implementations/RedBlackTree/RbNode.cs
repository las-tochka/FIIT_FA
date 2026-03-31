using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public enum RbColor : byte
{
    Red   = 0,
    Black = 1
}

public class RbNode<TKey, TValue>(TKey key, TValue value)
    : Node<TKey, TValue, RbNode<TKey, TValue>>(key, value)
{
    public RbColor Color { get; set; } = RbColor.Red;

    // Backward/algorithm compatibility: some implementations expect `RbColor` naming.
    public RbColor RbColor
    {
        get => Color;
        set => Color = value;
    }

    public bool IsRed => Color == RbColor.Red;
    public bool IsBlack => Color == RbColor.Black;
}