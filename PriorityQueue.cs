public class PriorityQueue<T> where T : NPC
{
    private List<T> _items = new List<T>();

    public int Count => _items.Count;

    public void Enqueue(T item)
    {
        _items.Add(item);
        _items = _items.OrderBy(i => i.NextTickTime).ToList();
    }

    public T? Dequeue()
    {
        if (Count == 0) return default;
        T item = _items[0];
        _items.RemoveAt(0);
        return item;
    }

    public T? Peek()
    {
        if (Count == 0) return default;
        return _items[0];
    }
    
    public List<T> ToList()
    {
        return new List<T>(_items);
    }
}