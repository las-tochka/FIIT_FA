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
        // просто замена
        if (node.Left == null) {
            Transplant(node, node.Right);
            OnNodeRemoved(node.Parent, node.Right);
        }
        else if (node.Right == null) {
            Transplant(node, node.Left);
            OnNodeRemoved(node.Parent, node.Left);
        }
        else {
            // ищем минимум в правом поддереве
            TNode successor = node.Right;
            while (successor.Left != null) {
                successor = successor.Left;
            }

            if (successor.Parent != node) {
                // замена
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                successor.Right!.Parent = successor;
            }

            Transplant(node, successor);
            successor.Left = node.Left;
            successor.Left!.Parent = successor;

            // удаление структурное
            OnNodeRemoved(successor.Parent, successor);
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

    protected int GetHeight(TNode? node)
    {
        if (node == null) return 0;
        return 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }
    
    
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

        // выясним место y в этой жизни
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
        if (x.Left == null) return;
        RotateRight(x.Left);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y.Right == null) return;
        RotateLeft(y.Right);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        if (x == null) return;

        RotateLeft(x);
        RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        if (y == null) return;

        RotateRight(y);
        RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null) {
            Root = v;
        }
        else if (u.IsLeftChild) {
            u.Parent.Left = v;
        } else {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(this, Root, TraversalStrategy.InOrder);
    
    
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => 
        new TreeIterator(this, Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => 
        new TreeIterator(this, Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => 
        new TreeIterator(this, Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => 
        new TreeIterator(this, Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => 
        new TreeIterator(this, Root, TraversalStrategy.PostOrderReverse);
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;

        private Stack<TNode> _stack;
        private TNode? _current;
        public TreeIterator(
            BinarySearchTreeBase<TKey, TValue, TNode> tree,
            TNode? root,
            TraversalStrategy strategy)
        {
            _tree = tree;
            _root = root;
            _strategy = strategy;

            _stack = new Stack<TNode>();
            _current = null;

            InitializeStack();
        }

        private void InitializeStack()
        {
            switch (_strategy)
            {
                case TraversalStrategy.InOrder:
                case TraversalStrategy.InOrderReverse:
                    PushLeft(_root);
                    break;
                case TraversalStrategy.PreOrder:
                    if (_root != null)
                        _stack.Push(_root);
                    break;
                case TraversalStrategy.PreOrderReverse:
                    if (_root != null)
                        _stack.Push(_root);
                    break;
                case TraversalStrategy.PostOrder:
                    InitializePostOrder();
                    break;
                case TraversalStrategy.PostOrderReverse:
                    InitializePostOrderReverse();
                    break;
            }
        }

        private void PushLeft(TNode? node) {
            while (node != null)
            {
                _stack.Push(node);
                node = node.Left;
            }
        }

            private void PushRight(TNode? node)
        {
            while (node != null)
            {
                _stack.Push(node);
                node = node.Right;
            }
        }

        private void InitializePostOrder()
        {
            if (_root == null) return;
            
            var stack = new Stack<TNode>();
            stack.Push(_root);
            
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                _stack.Push(node);
                
                if (node.Left != null)
                    stack.Push(node.Left);
                if (node.Right != null)
                    stack.Push(node.Right);
            }
        }
        
        private void InitializePostOrderReverse()
        {
            if (_root == null) return;
            
            var stack = new Stack<TNode>();
            stack.Push(_root);
            
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                _stack.Push(node);
                
                if (node.Right != null)
                    stack.Push(node.Right);
                if (node.Left != null)
                    stack.Push(node.Left);
            }
        }

        // private readonly BinarySearchTreeBase<TKey, TValue, TNode> _tree;
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current =>
            new TreeEntry<TKey, TValue>(
                _current!.Key,
                _current.Value,
                _tree.GetHeight(_current)
            );
        object IEnumerator.Current => Current;
        
        
        public bool MoveNext()
        {
            if (_stack.Count == 0)
                return false;

            switch (_strategy)
            {
                case TraversalStrategy.InOrder:
                    return MoveNextInOrder();
                case TraversalStrategy.InOrderReverse:
                    return MoveNextInOrderReverse();
                case TraversalStrategy.PreOrder:
                    return MoveNextPreOrder();
                case TraversalStrategy.PreOrderReverse:
                    return MoveNextPreOrderReverse();
                case TraversalStrategy.PostOrder:
                case TraversalStrategy.PostOrderReverse:
                    return MoveNextPostOrder();
                default:
                    throw new NotImplementedException($"Strategy {_strategy} not implemented");
            }
        }
        private bool MoveNextInOrder()
        {
            _current = _stack.Pop();
            
            if (_current!.Right != null)
                PushLeft(_current.Right);
            
            return true;
        }
        private bool MoveNextInOrderReverse()
        {
            _current = _stack.Pop();
            
            if (_current!.Left != null)
                PushRight(_current.Left);
            
            return true;
        }

        private bool MoveNextPreOrder()
        {
            _current = _stack.Pop();
            
            if (_current!.Right != null)
                _stack.Push(_current.Right);
            if (_current.Left != null)
                _stack.Push(_current.Left);
            
            return true;
        }

        private bool MoveNextPreOrderReverse()
        {
            _current = _stack.Pop();
            
            if (_current!.Left != null)
                _stack.Push(_current.Left);
            if (_current.Right != null)
                _stack.Push(_current.Right);
            
            return true;
        }

        private bool MoveNextPostOrder()
        {
            _current = _stack.Pop();
            return true;
        }
        
        public void Reset()
        {
            _stack.Clear();
            _current = null;

            if (_strategy == TraversalStrategy.InOrder)
            {
                PushLeft(_root);
            }
        }

        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var iterator = new TreeIterator(this, Root, TraversalStrategy.InOrder);

        while (iterator.MoveNext())
        {
            var entry = iterator.Current;
            yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        /// <summary>
    /// Поиск минимального ключа в дереве
    /// </summary>
    public virtual KeyValuePair<TKey, TValue> Min()
    {
        if (Root == null)
            throw new InvalidOperationException("Tree is empty");
        
        var node = Root;
        while (node.Left != null)
            node = node.Left;
        
        return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
    }

    /// <summary>
    /// Поиск максимального ключа в дереве
    /// </summary>
    public virtual KeyValuePair<TKey, TValue> Max()
    {
        if (Root == null)
            throw new InvalidOperationException("Tree is empty");
        
        var node = Root;
        while (node.Right != null)
            node = node.Right;
        
        return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
    }

    /// <summary>
    /// Проверка свойства бинарного дерева поиска
    /// </summary>
    public virtual bool IsValidBST()
    {
        return IsValidBST(Root, default, default, false, false);
    }

    private bool IsValidBST(TNode? node, TKey? minKey, TKey? maxKey, bool hasMin, bool hasMax)
    {
        if (node == null)
            return true;

        // Проверка текущего узла
        if (hasMin && Comparer.Compare(node.Key, minKey) <= 0)
            return false;
        if (hasMax && Comparer.Compare(node.Key, maxKey) >= 0)
            return false;

        // Рекурсивная проверка поддеревьев
        return IsValidBST(node.Left, minKey, node.Key, hasMin, true) &&
               IsValidBST(node.Right, node.Key, maxKey, true, hasMax);
    }

    /// <summary>
    /// Получение высоты дерева
    /// </summary>
    public int Height => GetHeight(Root);
}