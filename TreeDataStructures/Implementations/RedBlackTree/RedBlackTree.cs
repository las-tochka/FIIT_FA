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
            else // обратная ситуация
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
                
                // Случай 2: X — левый ребенок (RL)
                if (current == parent.Left)
                {
                    RotateRight(parent);
                    current = parent;
                    parent = current.Parent!;
                }
                
                // Случай 3: X — правый ребенок (RR)
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
        // Начинаем с child (узел, который встал на место удаленного)
        // Если child == null, берем parent
        var node = child ?? parent;
        
        // Если node == null или node красный, просто красим его в черный
        if (node == null)
        {
            return;
        }
        
        // Если node красный, просто делаем его черным (восстанавливаем черную высоту)
        if (node.IsRed)
        {
            node.Color = RbColor.Black;
            return;
        }
        
        // Балансировка для черного узла (двойная чернота)
        while (node != Root && node.IsBlack)
        {
            if (node == node.Parent?.Left)
            {
                var sibling = node.Parent.Right;
                
                // Случай 1: Брат красный
                if (sibling?.IsRed == true)
                {
                    sibling.Color = RbColor.Black;
                    node.Parent.Color = RbColor.Red;
                    RotateLeft(node.Parent);
                    sibling = node.Parent?.Right;
                }
                
                // Случай 2: Оба ребенка брата черные
                if ((sibling?.Left?.IsBlack ?? true) && (sibling?.Right?.IsBlack ?? true))
                {
                    if (sibling != null) sibling.Color = RbColor.Red;
                    node = node.Parent;
                }
                else
                {
                    // Случай 3: Правый ребенок брата черный
                    if (sibling?.Right?.IsBlack ?? true)
                    {
                        if (sibling?.Left != null) sibling.Left.Color = RbColor.Black;
                        if (sibling != null) sibling.Color = RbColor.Red;
                        RotateRight(sibling);
                        sibling = node.Parent?.Right;
                    }
                    
                    // Случай 4: Правый ребенок брата красный
                    if (sibling != null)
                    {
                        sibling.Color = node.Parent?.Color ?? RbColor.Black;
                        if (node.Parent != null) node.Parent.Color = RbColor.Black;
                        if (sibling.Right != null) sibling.Right.Color = RbColor.Black;
                        RotateLeft(node.Parent);
                    }
                    node = Root!;
                }
            }
            else // Зеркальные случаи (node == node.Parent.Right)
            {
                var sibling = node.Parent?.Left;
                
                // Случай 1: Брат красный
                if (sibling?.IsRed == true)
                {
                    sibling.Color = RbColor.Black;
                    if (node.Parent != null) node.Parent.Color = RbColor.Red;
                    RotateRight(node.Parent);
                    sibling = node.Parent?.Left;
                }
                
                // Случай 2: Оба ребенка брата черные
                if ((sibling?.Left?.IsBlack ?? true) && (sibling?.Right?.IsBlack ?? true))
                {
                    if (sibling != null) sibling.Color = RbColor.Red;
                    node = node.Parent;
                }
                else
                {
                    // Случай 3: Левый ребенок брата черный
                    if (sibling?.Left?.IsBlack ?? true)
                    {
                        if (sibling?.Right != null) sibling.Right.Color = RbColor.Black;
                        if (sibling != null) sibling.Color = RbColor.Red;
                        RotateLeft(sibling);
                        sibling = node.Parent?.Left;
                    }
                    
                    // Случай 4: Левый ребенок брата красный
                    if (sibling != null)
                    {
                        sibling.Color = node.Parent?.Color ?? RbColor.Black;
                        if (node.Parent != null) node.Parent.Color = RbColor.Black;
                        if (sibling.Left != null) sibling.Left.Color = RbColor.Black;
                        RotateRight(node.Parent);
                    }
                    node = Root!;
                }
            }
        }
        
        if (node != null)
            node.Color = RbColor.Black;
    }
}