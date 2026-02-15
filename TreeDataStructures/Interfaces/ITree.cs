namespace TreeDataStructures.Interfaces;

public interface ITree<TKey, TValue> : IDictionary<TKey, TValue>
{
    // Прямой порядок
    IEnumerable<KeyValuePair<TKey, TValue>> InOrder();   // Infix
    IEnumerable<KeyValuePair<TKey, TValue>> PreOrder();  // Prefix
    IEnumerable<KeyValuePair<TKey, TValue>> PostOrder(); // Postfix
    
    // Обратный порядок
    IEnumerable<KeyValuePair<TKey, TValue>> InOrderReverse();  // Infix Reverse
    IEnumerable<KeyValuePair<TKey, TValue>> PreOrderReverse(); // Prefix Reverse
    IEnumerable<KeyValuePair<TKey, TValue>> PostOrderReverse(); // Postfix Reverse
}