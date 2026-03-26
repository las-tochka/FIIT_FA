using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default;

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>();
            foreach (var entry in InOrder())
            {
                keys.Add(entry.Key);
            }
            return keys;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            var values = new List<TValue>();
            foreach (var entry in InOrder())
            {
                values.Add(entry.Value);
            }
            return values;
        }
    }
    public virtual void Add(TKey key, TValue value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        
        if (Root == null) {
            Root = CreateNode(key, value);
            Count++;
            OnNodeAdded(Root);
            return;
        }

        TNode? current = Root;
        TNode? parent = null;

        while (current != null) {
            parent = current;
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) {
                current.Value = value;
                return;
            }
            current = cmp < 0 ? current.Left : current.Right;
        }
        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;
        if (Comparer.Compare(key, parent!.Key) < 0)
            parent.Left = newNode;
        else
            parent.Right = newNode;
        Count++;
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null)
        {
            var parent = node.Parent;
            Transplant(node, node.Right);
            OnNodeRemoved(parent, node.Right);
        }
        else if (node.Right == null)
        {
            var parent = node.Parent;
            Transplant(node, node.Left);
            OnNodeRemoved(parent, node.Left);
        }
        else
        {
            TNode successor = node.Right;
            while (successor.Left != null)
                successor = successor.Left;

            if (successor.Parent != node)
            {
                var succParent = successor.Parent;
                Transplant(successor, successor.Right);
                OnNodeRemoved(succParent, successor.Right);

                successor.Right = node.Right;
                successor.Right!.Parent = successor;
            }

            var parent = node.Parent;
            Transplant(node, successor);
            successor.Left = node.Left;
            successor.Left!.Parent = successor;

            OnNodeRemoved(parent, successor);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x == null) throw new ArgumentNullException(nameof(x));
        TNode? y = x.Right;
        if (y == null) return;
        // разделяем
        x.Right = y.Left;
        if (y.Left != null){
            y.Left.Parent = x;
        }
        y.Parent = x.Parent;
        if (x.Parent == null){
            Root = y;
        } else if (x.IsLeftChild) {
            x.Parent.Left = y;
        } else {
            x.Parent.Right = y;
        }
        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        if (y == null) throw new ArgumentNullException(nameof(y));

        // nullable ссылка
        TNode? x = y.Left;
        if (x == null) return;
        y.Left = x.Right;
        if (x.Right != null)
            x.Right.Parent = y;

        x.Parent = y.Parent;

        if (y.Parent == null)
            Root = x;
        else if (y.IsLeftChild)
            y.Parent.Left = x;
        else
            y.Parent.Right = x;

        x.Right = y;
        y.Parent = x;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x?.Right == null) return;

        RotateRight(x.Right);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y?.Left == null) return;

        RotateLeft(y.Left);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        if (x?.Right == null) return;

        RotateLeft(x.Right);
        RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        if (y?.Left == null) return;

        RotateRight(y.Left);
        RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion

    private static int GetDepth(TNode node)
    {
        int depth = 0;

        while (node.Parent != null)
        {
            depth++;
            node = node.Parent;
        }

        return depth;
    }
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.InOrder);
    }

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.PreOrder);
    }

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder()
    {
        return new TreeIterator(Root, TraversalStrategy.PostOrder);
    }

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    }

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    }

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse()
    {
        return new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    }
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban) и без стека.
    /// </summary>
    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TraversalStrategy _strategy;
        private readonly TNode? _root;
        private TNode? _current;
        private TNode? _lastVisited;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _lastVisited = null;
            _current = GetFirst(root);
        }
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current =>
            new TreeEntry<TKey, TValue>(
                _lastVisited!.Key,
                _lastVisited.Value,
                GetDepth(_lastVisited)
            );

        object IEnumerator.Current => Current;
        
        public bool MoveNext()
        {
            if (_current == null) return false;
            
            _lastVisited = _current;
            _current = GetNext(_current);
            return true;
        }

        private TNode? GetFirst(TNode? root) => _strategy switch
        {
            TraversalStrategy.InOrder => Leftmost(root),
            TraversalStrategy.InOrderReverse => Rightmost(root),
            TraversalStrategy.PreOrder => root,
            TraversalStrategy.PreOrderReverse => GetFirstPreOrderReverse(root),
            TraversalStrategy.PostOrder => FirstPostOrder(root),
            TraversalStrategy.PostOrderReverse => root,
            _ => root
        };

        private TNode? GetNext(TNode node) => _strategy switch
        {
            TraversalStrategy.InOrder => NextInOrder(node),
            TraversalStrategy.InOrderReverse => NextInOrderReverse(node),
            TraversalStrategy.PreOrder => NextPreOrder(node),
            TraversalStrategy.PreOrderReverse => NextPreOrderReverse(node),
            TraversalStrategy.PostOrder => NextPostOrder(node),
            TraversalStrategy.PostOrderReverse => NextPostOrderReverse(node),
            _ => null
        };

        private static TNode? Leftmost(TNode? node)
        {
            while (node?.Left != null) node = node.Left;
            return node;
        }
        
        private static TNode? Rightmost(TNode? node)
        {
            while (node?.Right != null) node = node.Right;
            return node;
        }

        private static TNode? NextInOrder(TNode node)
        {
            if (node.Right != null) return Leftmost(node.Right);
            
            var current = node;
            var parent = current.Parent;
            while (parent != null && current == parent.Right)
            {
                current = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        private static TNode? NextInOrderReverse(TNode node)
        {
            if (node.Left != null) return Rightmost(node.Left);
            
            var current = node;
            var parent = current.Parent;
            while (parent != null && current == parent.Left)
            {
                current = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        private static TNode? NextPreOrder(TNode node)
        {
            if (node.Left != null) return node.Left;
            if (node.Right != null) return node.Right;
            
            var current = node;
            var parent = current.Parent;
            while (parent != null)
            {
                if (current == parent.Left && parent.Right != null)
                    return parent.Right;
                current = parent;
                parent = parent.Parent;
            }
            return null;
        }

        private static TNode? GetFirstPreOrderReverse(TNode? root)
        {
            if (root == null) return null;
            
            var current = root;
            while (current != null)
            {
                if (current.Right != null)
                    current = current.Right;
                else if (current.Left != null)
                    current = current.Left;
                else
                    return current;
            }
            return null;
        }

    private static TNode? NextPreOrderReverse(TNode node)
    {
        var parent = node.Parent;
        if (parent == null) return null;
        
        if (node == parent.Right)
        {
            if (parent.Left != null)
                return GetFirstPreOrderReverse(parent.Left);
            return parent;
        }
        
        if (node == parent.Left)
        {
            return parent;
        }
        
        return null;
    }

        private static TNode? FirstPostOrder(TNode? root)
        {
            var node = root;
            while (node != null)
            {
                if (node.Left != null) node = node.Left;
                else if (node.Right != null) node = node.Right;
                else return node;
            }
            return null;
        }
        
        private static TNode? NextPostOrder(TNode node)
        {
            var parent = node.Parent;
            if (parent == null) return null;
            
            if (node == parent.Left && parent.Right != null)
                return FirstPostOrder(parent.Right);
            
            return parent;
        }
        
        private static TNode? NextPostOrderReverse(TNode node)
        {
            if (node.Right != null) return node.Right;
            if (node.Left != null) return node.Left;
            
            var current = node;
            var parent = current.Parent;
            while (parent != null)
            {
                if (current == parent.Right && parent.Left != null)
                    return parent.Left;
                current = parent;
                parent = parent.Parent;
            }
            return null;
        }
        
        public void Reset()
        {
            _lastVisited = null;
            _current = GetFirst(_root);
        }

        
        public void Dispose()
        {
            // no resources to dispose
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var iterator = new TreeIterator(Root, TraversalStrategy.InOrder);
        return new Enumerator(iterator);
    }

    private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly IEnumerator<TreeEntry<TKey, TValue>> _enumerator;
        
        public Enumerator(IEnumerator<TreeEntry<TKey, TValue>> enumerator) 
            => _enumerator = enumerator;
        
        public KeyValuePair<TKey, TValue> Current => 
            new(_enumerator.Current.Key, _enumerator.Current.Value);
        
        object IEnumerator.Current => Current;
        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => _enumerator.Reset();
        public void Dispose() => _enumerator.Dispose();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach (var entry in InOrder())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}