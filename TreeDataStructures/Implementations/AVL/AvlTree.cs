using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    private int GetHeight(AvlNode<TKey, TValue>? node)
    {
        return node?.Height ?? 0;
    }

    private void UpdateHight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }

    private int GetBalance(AvlNode<TKey, TValue>? node)
    {
        return node == null ? 0 : GetHeight(node.Left) - GetHeight(node.Right);
    }
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var curNode = newNode.Parent;
        while (curNode != null)
        {
            UpdateHight(curNode);
            int balance = GetBalance(curNode);
            if (balance > 1 || balance < -1)
            {
                curNode = Rebalance(curNode);
            }

            curNode = curNode.Parent;
        }
    }

    private AvlNode<TKey, TValue> Rebalance(AvlNode<TKey, TValue> node)
    {
        int balance = GetBalance(node);
        if (balance < -1) // right больше(гарантия его существованя)
        {
            if (GetBalance(node.Right!) > 0)
            {
                RotateRight(node.Right!);
            }

            RotateLeft(node);

            UpdateHight(node);
            UpdateHight(node.Parent!);

            return node.Parent!;
        }

        if (balance > 1) // left больше(гарантия его существованя)
        {
            if (GetBalance(node.Left!) < 0)
            {
                RotateLeft(node.Left!);
            }

            RotateRight(node);

            UpdateHight(node);
            UpdateHight(node.Parent!);

            return node.Parent!;
        }

        return node;
    }
    
}