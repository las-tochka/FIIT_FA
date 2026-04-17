using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        newNode.RbColor = RbColor.Red;

        var current = newNode;
        while (current != null && current.Parent?.IsRed == true)
        {
            var parent = current.Parent!;
            var grandPa = parent.Parent!;

            if (parent == grandPa.Left)
            {
                var uncle = grandPa.Right;
                if (uncle?.IsRed == true)
                {
                    parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    grandPa.Color = RbColor.Red;
                    current = grandPa;
                    continue;
                }

                if (current == parent.Right)
                {
                    RotateLeft(parent);
                    current = parent;
                    parent = current.Parent!;
                }

                RotateRight(grandPa);
                parent.Color = RbColor.Black;
                grandPa.Color = RbColor.Red;
                break;
            }
            else
            {
                var uncle = grandPa.Left;
                if (uncle?.IsRed == true)
                {
                    parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    grandPa.Color = RbColor.Red;
                    current = grandPa;
                    continue;
                }
                
                if (current == parent.Left)
                {
                    RotateRight(parent);
                    current = parent;
                    parent = current.Parent!;
                }
                
                RotateLeft(grandPa);
                parent.Color = RbColor.Black;
                grandPa.Color = RbColor.Red;
                break;
            }
        }
        if (Root != null)
        {
            Root.Color = RbColor.Black;
        }
    }
    
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        var node = child ?? parent;
        if (node == null)
        {
            return;
        }

        if (node.IsRed)
        {
            node.Color = RbColor.Black;
            return;
        }
        while (node != null && node != Root && node.IsBlack)
        {
            if (node == node.Parent?.Left)
            {
                var brother = node.Parent.Right;
                
                if (brother?.IsRed == true)
                {
                    brother.Color = RbColor.Black;
                    node.Parent.Color = RbColor.Red;
                    if (node.Parent != null) RotateLeft(node.Parent);
                    brother = node.Parent?.Right;
                }
                
                if ((brother?.Left?.IsBlack ?? true) && (brother?.Right?.IsBlack ?? true))
                {
                    if (brother != null) brother.Color = RbColor.Red;
                    node = node.Parent;
                }
                else
                {
                    if (brother?.Right?.IsBlack ?? true)
                    {
                        if (brother?.Left != null) brother.Left.Color = RbColor.Black;
                        if (brother != null) brother.Color = RbColor.Red;
                        if (brother != null) RotateRight(brother);
                        brother = node.Parent?.Right;
                    }
                    
                    if (brother != null)
                    {
                        brother.Color = node.Parent?.Color ?? RbColor.Black;
                        if (node.Parent != null) node.Parent.Color = RbColor.Black;
                        if (brother.Right != null) brother.Right.Color = RbColor.Black;
                        if (node.Parent != null) RotateLeft(node.Parent);
                    }
                    node = Root!;
                }
            }
            else
            {
                var brother = node.Parent?.Left;
                
                if (brother?.IsRed == true)
                {
                    brother.Color = RbColor.Black;
                    if (node.Parent != null) node.Parent.Color = RbColor.Red;
                    if (node.Parent != null) RotateRight(node.Parent);
                    brother = node.Parent?.Left;
                }
                
                if ((brother?.Left?.IsBlack ?? true) && (brother?.Right?.IsBlack ?? true))
                {
                    if (brother != null) brother.Color = RbColor.Red;
                    node = node.Parent;
                }
                else
                {
                    if (brother?.Left?.IsBlack ?? true)
                    {
                        if (brother?.Right != null) brother.Right.Color = RbColor.Black;
                        if (brother != null) brother.Color = RbColor.Red;
                        if (brother != null) RotateLeft(brother);
                        brother = node.Parent?.Left;
                    }
                    
                    if (brother != null)
                    {
                        brother.Color = node.Parent?.Color ?? RbColor.Black;
                        if (node.Parent != null) node.Parent.Color = RbColor.Black;
                        if (brother.Left != null) brother.Left.Color = RbColor.Black;
                        if (node.Parent != null) RotateRight(node.Parent);
                    }
                    node = Root!;
                }
            }
        }
        
        if (node != null)
            node.Color = RbColor.Black;
    }
}