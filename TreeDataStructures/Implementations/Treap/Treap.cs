using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    private int _nextPriority = int.MaxValue;

    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
            return (null, null);

        if (Comparer.Compare(root.Key, key) <= 0)
        {
            var (l, r) = Split(root.Right, key);

            root.Right = l;
            if (l != null) l.Parent = root;
            if (r != null) r.Parent = null;

            return (root, r);
        }
        else
        {
            var (l, r) = Split(root.Left, key);

            root.Left = r;
            if (r != null) r.Parent = root;
            if (l != null) l.Parent = null;

            return (l, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(
        TreapNode<TKey, TValue>? left,
        TreapNode<TKey, TValue>? right)
    {
        if (left == null)
            return right;

        if (right == null)
            return left;

        if (left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);

            if (left.Right != null)
                left.Right.Parent = left;

            return left;
        }
        else
        {
            right.Left = Merge(left, right.Left);

            if (right.Left != null)
                right.Left.Parent = right;

            return right;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        var existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }
        var newNode = CreateNode(key, value);
        newNode.Priority = _nextPriority--;

        var (left, right) = Split(Root, key);

        var merged = Merge(left, newNode);
        Root = Merge(merged, right);

        if (Root != null)
            Root.Parent = null;
        Count++;
    }

    public override bool Remove(TKey key)
    {
        var node = FindNode(key);
        if (node == null)
            return false;

        TreapNode<TKey, TValue>? mergedChildren = Merge(node.Left, node.Right);

        if (mergedChildren != null)
            mergedChildren.Parent = node.Parent;

        if (node.Parent == null)
        {
            Root = mergedChildren;
        }
        else if (node.IsLeftChild)
        {
            node.Parent.Left = mergedChildren;
        }
        else
        {
            node.Parent.Right = mergedChildren;
        }

        if (Root != null)
            Root.Parent = null;
        Count--;
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
        // Not used in Split/Merge implementation
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
        // Not used in Split/Merge implementation
    }
    
}