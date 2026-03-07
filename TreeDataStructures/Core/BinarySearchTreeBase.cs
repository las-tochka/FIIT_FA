using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

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
            if (cmp == 0) { // уже есть
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
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => InOrderTraversal(Root);
    
    private IEnumerable<TreeEntry<TKey, TValue>> InOrderTraversal(TNode? node)
    {
        if (node == null)
            yield break;

        foreach (var entry in InOrderTraversal(node.Left))
            yield return entry;

        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, GetDepth(node));

        foreach (var entry in InOrderTraversal(node.Right))
            yield return entry;
    }
    
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() => PreOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PreOrderTraversal(TNode? node)
    {
        if (node == null)
            yield break;

        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, GetDepth(node));

        foreach (var entry in PreOrderTraversal(node.Left))
            yield return entry;

        foreach (var entry in PreOrderTraversal(node.Right))
            yield return entry;
    }

    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() => PostOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PostOrderTraversal(TNode? node)
    {
        if (node == null)
            yield break;

        foreach (var entry in PostOrderTraversal(node.Left))
            yield return entry;

        foreach (var entry in PostOrderTraversal(node.Right))
            yield return entry;

        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, GetDepth(node));
    }

    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() => InOrderReverseTraversal(Root);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverseTraversal(TNode? node)
    {
        var stack = new Stack<TreeEntry<TKey, TValue>>();

        foreach (var entry in InOrder())
            stack.Push(entry);

        while (stack.Count > 0)
            yield return stack.Pop();
    }

    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() => PreOrderReverseTraversal(Root);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverseTraversal(TNode? node)
    {
        var stack = new Stack<TreeEntry<TKey, TValue>>();

        foreach (var entry in PreOrder())
            stack.Push(entry);

        while (stack.Count > 0)
            yield return stack.Pop();
    }

    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse() => PostOrderReverseTraversal(Root);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverseTraversal(TNode? node)
    {
        var stack = new Stack<TreeEntry<TKey, TValue>>();

        foreach (var entry in PostOrder())
            stack.Push(entry);

        while (stack.Count > 0)
            yield return stack.Pop();
    }
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TraversalStrategy _strategy;
        private readonly TNode? _root;
        private Stack<TNode> _stack;
        private TNode? _currentNode;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _stack = new Stack<TNode>();
            _currentNode = root;
        }
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current =>
            new TreeEntry<TKey, TValue>(
                _currentNode!.Key,
                _currentNode.Value,
                GetDepth(_currentNode)
            );
        object IEnumerator.Current => Current;
        
        
        public bool MoveNext()
        {
            if (_strategy == TraversalStrategy.InOrder)
            {
                while (_currentNode != null)
                {
                    _stack.Push(_currentNode);
                    _currentNode = _currentNode.Left;
                }

                if (_stack.Count == 0)
                    return false;

                var node = _stack.Pop();
                _currentNode = node.Right;

                _currentNode = node;
                return true;
            }

            throw new NotImplementedException("Strategy not implemented");
        }
        
        public void Reset()
        {
            _stack.Clear();
            _currentNode = _root;
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

        foreach (var entry in iterator)
            yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
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