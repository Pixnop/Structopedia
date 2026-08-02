using System;
using System.Collections.Generic;

namespace Structopedia.Caching;

/// <summary>
/// Bounded cache that drops the least recently used entry once it is full, handing the dropped value
/// to a callback so the owner can release it.
/// <para>
/// Not thread safe by design: it only ever runs on the game main thread, alongside the GPU resources
/// it is meant to hold.
/// </para>
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Cached value type.</typeparam>
internal sealed class LruCache<TKey, TValue>
    where TKey : notnull
{
    /// <summary>Entries from the most to the least recently used.</summary>
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _order = new();

    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _nodes;
    private readonly Action<TValue> _onEvict;
    private readonly int _capacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="LruCache{TKey, TValue}"/> class, empty.
    /// </summary>
    /// <param name="capacity">Maximum number of entries held at once, at least one.</param>
    /// <param name="onEvict">Called with every value the cache lets go of.</param>
    internal LruCache(int capacity, Action<TValue> onEvict)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentNullException.ThrowIfNull(onEvict);

        _capacity = capacity;
        _onEvict = onEvict;
        _nodes = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(capacity);
    }

    /// <summary>Number of entries currently held.</summary>
    internal int Count => _nodes.Count;

    /// <summary>Looks a key up and marks it as the most recently used entry on a hit.</summary>
    /// <param name="key">Key to look up.</param>
    /// <param name="value">The cached value on a hit, the default value on a miss.</param>
    /// <returns>True on a hit.</returns>
    internal bool TryGet(TKey key, out TValue? value)
    {
        if (!_nodes.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue>>? node))
        {
            value = default;
            return false;
        }

        _order.Remove(node);
        _order.AddFirst(node);
        value = node.Value.Value;
        return true;
    }

    /// <summary>
    /// Stores a value as the most recently used entry. Replacing an existing key evicts the value it
    /// displaces, so the caller never has to track that itself.
    /// </summary>
    /// <param name="key">Key to store under.</param>
    /// <param name="value">Value to store.</param>
    internal void Set(TKey key, TValue value)
    {
        if (_nodes.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue>>? existing))
        {
            TValue replaced = existing.Value.Value;
            existing.Value = new KeyValuePair<TKey, TValue>(key, value);
            _order.Remove(existing);
            _order.AddFirst(existing);
            _onEvict(replaced);
            return;
        }

        _nodes[key] = _order.AddFirst(new KeyValuePair<TKey, TValue>(key, value));

        while (_nodes.Count > _capacity)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> oldest = _order.Last!;
            _order.RemoveLast();
            _nodes.Remove(oldest.Value.Key);
            _onEvict(oldest.Value.Value);
        }
    }

    /// <summary>Empties the cache, evicting every value from the least to the most recently used.</summary>
    internal void Clear()
    {
        // The callback is free to touch the cache, so the state is dropped before anything is called.
        var values = new List<TValue>(_order.Count);
        for (LinkedListNode<KeyValuePair<TKey, TValue>>? node = _order.Last; node != null; node = node.Previous)
        {
            values.Add(node.Value.Value);
        }

        _order.Clear();
        _nodes.Clear();

        foreach (TValue value in values)
        {
            _onEvict(value);
        }
    }
}
