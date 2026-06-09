using DskSpc.Models;

namespace DskSpc.Services;

public sealed class HistoryManager
{
    private readonly Queue<DriveSnapshot> _items;

    public HistoryManager(int capacity)
    {
        if (capacity <= 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        Capacity = capacity;
        _items = new Queue<DriveSnapshot>(capacity);
    }

    public int Count => _items.Count;

    public int Capacity { get; }

    public void Add(DriveSnapshot snapshot)
    {
        _items.Enqueue(snapshot);

        while (_items.Count > Capacity) {
            _items.Dequeue();
        }
    }

    public IReadOnlyList<DriveSnapshot> GetNewestFirst()
    {
        return _items.Reverse().ToList();
    }
}
