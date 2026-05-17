using System;
using System.Collections;
using System.Collections.Generic;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Collections;

public sealed class ReactiveDictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly IDictionary<TKey, TValue> _innerDictionary;
    private readonly DependencySlot _countSlot = new();
    private readonly DependencySlot _iterationSlot = new();
    private readonly Dictionary<TKey, DependencySlot> _keySlots;
    private readonly object _writeLock = new();

    private event Action<DictionaryChangeEvent<TKey, TValue>>? _changeHandlers;

    public ReactiveDictionaryWrapper(IDictionary<TKey, TValue> innerDictionary)
    {
        ArgumentNullException.ThrowIfNull(innerDictionary);
        _innerDictionary = innerDictionary;
        _keySlots = new Dictionary<TKey, DependencySlot>();
    }

    private DependencySlot GetOrCreateKeySlot(TKey key)
    {
        if (!_keySlots.TryGetValue(key, out DependencySlot? keySlot))
        {
            keySlot = new DependencySlot();
            _keySlots[key] = keySlot;
        }
        return keySlot;
    }

    public System.IObservable<DictionaryChangeEvent<TKey, TValue>> AsChangeObservable()
    {
        return new Interop.ChangeObservableAdapter<DictionaryChangeEvent<TKey, TValue>>(
            registerHandler: handler => _changeHandlers += handler,
            unregisterHandler: handler => _changeHandlers -= handler);
    }

    private void EmitChangeEvent(DictionaryChangeEvent<TKey, TValue> changeEvent)
    {
        _changeHandlers?.Invoke(changeEvent);
    }

    public TValue this[TKey key]
    {
        get
        {
            DependencySlot keySlot;
            lock (_writeLock) keySlot = GetOrCreateKeySlot(key);
            keySlot.RegisterCurrentEffectAsDependent();
            lock (_writeLock) return _innerDictionary[key];
        }
        set
        {
            bool wasNewKey;
            TValue previousValue;
            DependencySlot keySlot;
            lock (_writeLock)
            {
                wasNewKey = !_innerDictionary.TryGetValue(key, out previousValue!);
                _innerDictionary[key] = value;
                keySlot = GetOrCreateKeySlot(key);
            }
            keySlot.NotifyAllSubscribers();
            _iterationSlot.NotifyAllSubscribers();
            if (wasNewKey)
            {
                _countSlot.NotifyAllSubscribers();
                EmitChangeEvent(new DictionaryChangeEvent<TKey, TValue>.Added(key, value));
            }
            else
            {
                EmitChangeEvent(new DictionaryChangeEvent<TKey, TValue>.Replaced(key, previousValue, value));
            }
        }
    }

    public ICollection<TKey> Keys
    {
        get
        {
            _iterationSlot.RegisterCurrentEffectAsDependent();
            lock (_writeLock) return new List<TKey>(_innerDictionary.Keys);
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            _iterationSlot.RegisterCurrentEffectAsDependent();
            lock (_writeLock) return new List<TValue>(_innerDictionary.Values);
        }
    }

    public int Count
    {
        get
        {
            _countSlot.RegisterCurrentEffectAsDependent();
            lock (_writeLock) return _innerDictionary.Count;
        }
    }

    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value) => this[key] = value;

    public void Add(KeyValuePair<TKey, TValue> entryToAdd) => this[entryToAdd.Key] = entryToAdd.Value;

    public void Clear()
    {
        List<DependencySlot> slotsToNotify;
        lock (_writeLock)
        {
            _innerDictionary.Clear();
            slotsToNotify = new List<DependencySlot>(_keySlots.Values);
        }
        foreach (DependencySlot keySlot in slotsToNotify) keySlot.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
        EmitChangeEvent(new DictionaryChangeEvent<TKey, TValue>.Cleared());
    }

    public bool Contains(KeyValuePair<TKey, TValue> entryToCheck)
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) return _innerDictionary.Contains(entryToCheck);
    }

    public bool ContainsKey(TKey key)
    {
        DependencySlot keySlot;
        lock (_writeLock) keySlot = GetOrCreateKeySlot(key);
        keySlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) return _innerDictionary.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] destinationArray, int destinationArrayIndex)
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) _innerDictionary.CopyTo(destinationArray, destinationArrayIndex);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        KeyValuePair<TKey, TValue>[] snapshotArray;
        lock (_writeLock)
        {
            snapshotArray = new KeyValuePair<TKey, TValue>[_innerDictionary.Count];
            _innerDictionary.CopyTo(snapshotArray, 0);
        }
        return ((IEnumerable<KeyValuePair<TKey, TValue>>)snapshotArray).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Remove(TKey key)
    {
        bool removed;
        TValue removedValue;
        DependencySlot? keySlot;
        lock (_writeLock)
        {
            if (!_innerDictionary.TryGetValue(key, out removedValue!))
                return false;
            removed = _innerDictionary.Remove(key);
            _keySlots.TryGetValue(key, out keySlot);
        }
        if (!removed) return false;
        keySlot?.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        EmitChangeEvent(new DictionaryChangeEvent<TKey, TValue>.Removed(key, removedValue));
        return true;
    }

    public bool Remove(KeyValuePair<TKey, TValue> entryToRemove) => Remove(entryToRemove.Key);

    public bool TryGetValue(TKey key, out TValue value)
    {
        DependencySlot keySlot;
        lock (_writeLock) keySlot = GetOrCreateKeySlot(key);
        keySlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) return _innerDictionary.TryGetValue(key, out value!);
    }
}
