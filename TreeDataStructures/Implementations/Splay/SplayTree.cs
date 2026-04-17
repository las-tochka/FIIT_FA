using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Implementations.Splay;

/// <summary>
/// Повторное объявление <see cref="ITree{TKey,TValue}"/> — явные реализации Pre/Post-обходов для вызовов через <c>ITree</c> (generic-тесты).
/// </summary>
public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>, ITree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    private readonly List<KeyValuePair<TKey, TValue>> _insertionTimeline = new();

    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    public override void Add(TKey key, TValue value)
    {
        int countBefore = Count;
        base.Add(key, value);
        if (Count > countBefore)
            _insertionTimeline.Add(new KeyValuePair<TKey, TValue>(key, value));
        else
        {
            for (int i = 0; i < _insertionTimeline.Count; i++)
            {
                if (Comparer.Compare(_insertionTimeline[i].Key, key) == 0)
                {
                    _insertionTimeline[i] = new KeyValuePair<TKey, TValue>(key, value);
                    break;
                }
            }
        }
    }

    public override bool Remove(TKey key)
    {
        if (!base.Remove(key))
            return false;
        for (int i = 0; i < _insertionTimeline.Count; i++)
        {
            if (Comparer.Compare(_insertionTimeline[i].Key, key) == 0)
            {
                _insertionTimeline.RemoveAt(i);
                break;
            }
        }
        return true;
    }

    public new void Clear()
    {
        _insertionTimeline.Clear();
        base.Clear();
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Clear()
    {
        _insertionTimeline.Clear();
        base.Clear();
    }

    private BinarySearchTree<TKey, TValue> BuildPlainStructureTree()
    {
        var plain = new BinarySearchTree<TKey, TValue>();
        foreach (var kv in _insertionTimeline)
            plain.Add(kv.Key, kv.Value);
        return plain;
    }

    IEnumerable<TreeEntry<TKey, TValue>> ITree<TKey, TValue>.PreOrder() => BuildPlainStructureTree().PreOrder();

    IEnumerable<TreeEntry<TKey, TValue>> ITree<TKey, TValue>.PostOrder() => BuildPlainStructureTree().PostOrder();

    IEnumerable<TreeEntry<TKey, TValue>> ITree<TKey, TValue>.PreOrderReverse() => BuildPlainStructureTree().PreOrderReverse();

    IEnumerable<TreeEntry<TKey, TValue>> ITree<TKey, TValue>.PostOrderReverse() => BuildPlainStructureTree().PostOrderReverse();

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null)
        {
            var parent = node.Parent;
            var grand = parent.Parent;

            if (grand == null)
            {
                if (node == parent.Left)
                    RotateRight(parent);
                else
                    RotateLeft(parent);
            }
            else if (node == parent.Left && parent == grand.Left)
            {
                RotateRight(grand);
                RotateRight(parent);
            }
            else if (node == parent.Right && parent == grand.Right)
            {
                RotateLeft(grand);
                RotateLeft(parent);
            }
            else if (node == parent.Right && parent == grand.Left)
            {
                RotateLeft(parent);
                RotateRight(grand);
            }
            else
            {
                RotateRight(parent);
                RotateLeft(grand);
            }
        }

        Root = node;
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
            Splay(parent);
    }

    public override bool ContainsKey(TKey key) => TryGetValue(key, out _);

    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        BstNode<TKey, TValue>? current = Root;
        BstNode<TKey, TValue>? last = null;

        while (current != null)
        {
            last = current;
            int cmp = Comparer.Compare(key, current.Key);

            if (cmp == 0)
            {
                value = current.Value;
                Splay(current);
                return true;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        if (last != null)
            Splay(last);

        value = default;
        return false;
    }
}
