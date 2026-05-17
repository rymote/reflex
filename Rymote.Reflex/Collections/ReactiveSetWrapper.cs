using System;
using System.Collections;
using System.Collections.Generic;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Collections;

/// <summary>Reactive proxy over an <see cref="ISet{T}"/> that tracks per-value reads and notifies effects on mutation.</summary>
/// <typeparam name="TItem">Element type of the set.</typeparam>
public sealed class ReactiveSetWrapper<TItem> : ISet<TItem>
    where TItem : notnull
{
    private readonly ISet<TItem> _innerSet;
    private readonly DependencySlot _countSlot = new();
    private readonly DependencySlot _iterationSlot = new();
    private readonly Dictionary<TItem, DependencySlot> _valueSlots;
    private readonly object _writeLock = new();

    private event Action<SetChangeEvent<TItem>>? _changeHandlers;

    /// <summary>Initializes a new <see cref="ReactiveSetWrapper{TItem}"/> that proxies the given set.</summary>
    /// <param name="innerSet">The underlying BCL set to wrap.</param>
    public ReactiveSetWrapper(ISet<TItem> innerSet)
    {
        ArgumentNullException.ThrowIfNull(innerSet);
        _innerSet = innerSet;
        _valueSlots = new Dictionary<TItem, DependencySlot>();
    }

    private DependencySlot GetOrCreateValueSlot(TItem item)
    {
        if (!_valueSlots.TryGetValue(item, out DependencySlot? slot))
        {
            slot = new DependencySlot();
            _valueSlots[item] = slot;
        }
        return slot;
    }

    /// <summary>Returns an <see cref="System.IObservable{T}"/> that emits a <see cref="SetChangeEvent{TItem}"/> each time the set is mutated.</summary>
    public System.IObservable<SetChangeEvent<TItem>> AsChangeObservable()
    {
        return new Interop.ChangeObservableAdapter<SetChangeEvent<TItem>>(
            registerHandler: handler => _changeHandlers += handler,
            unregisterHandler: handler => _changeHandlers -= handler);
    }

    private void EmitChangeEvent(SetChangeEvent<TItem> changeEvent)
    {
        _changeHandlers?.Invoke(changeEvent);
    }

    public int InternalTrackedSlotCount => _valueSlots.Count;

    public int Count
    {
        get
        {
            _countSlot.RegisterCurrentEffectAsDependent();
            lock (_writeLock) return _innerSet.Count;
        }
    }

    public bool IsReadOnly => false;

    public bool Add(TItem item)
    {
        DependencySlot valueSlot;
        bool wasAdded;
        lock (_writeLock)
        {
            wasAdded = _innerSet.Add(item);
            valueSlot = GetOrCreateValueSlot(item);
        }
        if (!wasAdded) return false;
        valueSlot.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        EmitChangeEvent(new SetChangeEvent<TItem>.Added(item));
        return true;
    }

    void ICollection<TItem>.Add(TItem item) => Add(item);

    public void Clear()
    {
        List<DependencySlot> slotsToNotify;
        lock (_writeLock)
        {
            _innerSet.Clear();
            slotsToNotify = new List<DependencySlot>(_valueSlots.Values);
            _valueSlots.Clear();
        }
        foreach (DependencySlot slot in slotsToNotify) slot.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        EmitChangeEvent(new SetChangeEvent<TItem>.Cleared());
    }

    public bool Contains(TItem item)
    {
        DependencySlot valueSlot;
        lock (_writeLock) valueSlot = GetOrCreateValueSlot(item);
        valueSlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) return _innerSet.Contains(item);
    }

    public void CopyTo(TItem[] destinationArray, int destinationArrayIndex)
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) _innerSet.CopyTo(destinationArray, destinationArrayIndex);
    }

    public bool Remove(TItem item)
    {
        DependencySlot? valueSlot;
        bool wasRemoved;
        lock (_writeLock)
        {
            wasRemoved = _innerSet.Remove(item);
            _valueSlots.TryGetValue(item, out valueSlot);
            _valueSlots.Remove(item);
        }
        if (!wasRemoved) return false;
        valueSlot?.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        EmitChangeEvent(new SetChangeEvent<TItem>.Removed(item));
        return true;
    }

    public IEnumerator<TItem> GetEnumerator()
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        TItem[] snapshotArray;
        lock (_writeLock)
        {
            snapshotArray = new TItem[_innerSet.Count];
            _innerSet.CopyTo(snapshotArray, 0);
        }
        return ((IEnumerable<TItem>)snapshotArray).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void ExceptWith(IEnumerable<TItem> other) { foreach (TItem item in other) Remove(item); }
    public void IntersectWith(IEnumerable<TItem> other)
    {
        HashSet<TItem> otherAsSet = new(other);
        TItem[] currentItems;
        lock (_writeLock)
        {
            currentItems = new TItem[_innerSet.Count];
            _innerSet.CopyTo(currentItems, 0);
        }
        foreach (TItem item in currentItems)
            if (!otherAsSet.Contains(item)) Remove(item);
    }
    public bool IsProperSubsetOf(IEnumerable<TItem> other) { _iterationSlot.RegisterCurrentEffectAsDependent(); lock (_writeLock) return _innerSet.IsProperSubsetOf(other); }
    public bool IsProperSupersetOf(IEnumerable<TItem> other) { _iterationSlot.RegisterCurrentEffectAsDependent(); lock (_writeLock) return _innerSet.IsProperSupersetOf(other); }
    public bool IsSubsetOf(IEnumerable<TItem> other) { _iterationSlot.RegisterCurrentEffectAsDependent(); lock (_writeLock) return _innerSet.IsSubsetOf(other); }
    public bool IsSupersetOf(IEnumerable<TItem> other) { _iterationSlot.RegisterCurrentEffectAsDependent(); lock (_writeLock) return _innerSet.IsSupersetOf(other); }
    public bool Overlaps(IEnumerable<TItem> other) { _iterationSlot.RegisterCurrentEffectAsDependent(); lock (_writeLock) return _innerSet.Overlaps(other); }
    public bool SetEquals(IEnumerable<TItem> other) { _iterationSlot.RegisterCurrentEffectAsDependent(); lock (_writeLock) return _innerSet.SetEquals(other); }
    public void SymmetricExceptWith(IEnumerable<TItem> other)
    {
        foreach (TItem item in other)
            if (Contains(item)) Remove(item); else Add(item);
    }
    public void UnionWith(IEnumerable<TItem> other) { foreach (TItem item in other) Add(item); }
}
