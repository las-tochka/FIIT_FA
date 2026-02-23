namespace TreeDataStructures.Core;

public class Node<TKey, TValue, TNode>(TKey key, TValue value) where TNode : Node<TKey, TValue, TNode>
{
    public TKey Key { get; set; } = key; // читаем и меняем:)
    public TValue Value { get; set; } = value;// +инициализируем
    
    // дочерние узлы (? nulable-ссылка вроде указателя)
    // но без арифметики указателейи ручного делит(GC спасет мир)
    public TNode? Left { get; set; }
    public TNode? Right { get; set; }
    public TNode? Parent { get; set; }
    
    // геттер
    public bool IsLeftChild  => this.Parent != null && this.Parent.Left == this;
    public bool IsRightChild => this.Parent != null && this.Parent.Right == this;
}