using System;
using System.Collections;
using System.Collections.Generic;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Collections;

/// <summary>Reactive proxy over an <see cref="IList{T}"/> that tracks index-level and count-level reads and notifies effects on mutation.</summary>
/// <typeparam name="TItem">Element type of the list.</typeparam>
public sealed class ReactiveListWrapper<TItem> : IList<TItem>
{
    private readonly IList<TItem> _innerList;
    private readonly DependencySlot _countSlot = new();
    private readonly DependencySlot _iterationSlot = new();
    private readonly Dictionary<int, DependencySlot> _indexSlots = new();
    private readonly object _writeLock = new();

    private event Action<ListChangeEvent<TItem>>? _changeHandlers;

    /// <summary>Initializes a new <see cref="ReactiveListWrapper{TItem}"/> that proxies the given list.</summary>
    /// <param name="innerList">The underlying BCL list to wrap.</param>
    public ReactiveListWrapper(IList<TItem> innerList)
    {
        ArgumentNullException.ThrowIfNull(innerList);
        _innerList = innerList;
    }

    private DependencySlot GetOrCreateIndexSlot(int index)
    {
        if (!_indexSlots.TryGetValue(index, out DependencySlot? indexSlot))
        {
            indexSlot = new DependencySlot();
            _indexSlots[index] = indexSlot;
        }
        return indexSlot;
    }

    /// <summary>Returns an <see cref="System.IObservable{T}"/> that emits a <see cref="ListChangeEvent{TItem}"/> each time the list is mutated.</summary>
    public System.IObservable<ListChangeEvent<TItem>> AsChangeObservable()
    {
        return new Interop.ChangeObservableAdapter<ListChangeEvent<TItem>>(
            registerHandler: handler => _changeHandlers += handler,
            unregisterHandler: handler => _changeHandlers -= handler);
    }

    private void EmitChangeEvent(ListChangeEvent<TItem> changeEvent)
    {
        _changeHandlers?.Invoke(changeEvent);
    }

    public int InternalTrackedSlotCount => _indexSlots.Count;

    public int Count
    {
        get
        {
            _countSlot.RegisterCurrentEffectAsDependent();
            lock (_writeLock) return _innerList.Count;
        }
    }

    public bool IsReadOnly => false;

    public TItem this[int index]
    {
        get
        {
            DependencySlot indexSlot;
            lock (_writeLock) indexSlot = GetOrCreateIndexSlot(index);
            indexSlot.RegisterCurrentEffectAsDependent();
            lock (_writeLock) return _innerList[index];
        }
        set
        {
            TItem previousItem;
            DependencySlot indexSlot;
            lock (_writeLock)
            {
                previousItem = _innerList[index];
                _innerList[index] = value;
                indexSlot = GetOrCreateIndexSlot(index);
            }
            indexSlot.NotifyAllSubscribers();
            _iterationSlot.NotifyAllSubscribers();
            EmitChangeEvent(new ListChangeEvent<TItem>.Replaced(index, previousItem, value));
        }
    }

    public void Add(TItem item)
    {
        int newIndex;
        DependencySlot? slotForNewlyOccupiedIndex;
        lock (_writeLock)
        {
            newIndex = _innerList.Count;
            _innerList.Add(item);
            _indexSlots.TryGetValue(newIndex, out slotForNewlyOccupiedIndex);
        }
        slotForNewlyOccupiedIndex?.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
        EmitChangeEvent(new ListChangeEvent<TItem>.Added(newIndex, item));
    }

    public void Clear()
    {
        List<DependencySlot> slotsToNotify;
        lock (_writeLock)
        {
            _innerList.Clear();
            slotsToNotify = new List<DependencySlot>(_indexSlots.Values);
            _indexSlots.Clear();
        }
        foreach (DependencySlot indexSlot in slotsToNotify)
            indexSlot.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
        EmitChangeEvent(new ListChangeEvent<TItem>.Cleared());
    }

    public bool Contains(TItem item)
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) return _innerList.Contains(item);
    }

    public void CopyTo(TItem[] destinationArray, int destinationArrayIndex)
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) _innerList.CopyTo(destinationArray, destinationArrayIndex);
    }

    public IEnumerator<TItem> GetEnumerator()
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        TItem[] snapshotArray;
        lock (_writeLock)
        {
            snapshotArray = new TItem[_innerList.Count];
            _innerList.CopyTo(snapshotArray, 0);
        }
        return ((IEnumerable<TItem>)snapshotArray).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int IndexOf(TItem item)
    {
        _iterationSlot.RegisterCurrentEffectAsDependent();
        lock (_writeLock) return _innerList.IndexOf(item);
    }

    public void Insert(int index, TItem item)
    {
        List<DependencySlot> slotsToNotify;
        lock (_writeLock)
        {
            _innerList.Insert(index, item);
            slotsToNotify = new List<DependencySlot>();
            foreach (var entry in _indexSlots)
                if (entry.Key >= index) slotsToNotify.Add(entry.Value);
        }
        foreach (DependencySlot indexSlot in slotsToNotify)
            indexSlot.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
        EmitChangeEvent(new ListChangeEvent<TItem>.Added(index, item));
    }

    public bool Remove(TItem item)
    {
        int removedAtIndex;
        lock (_writeLock)
        {
            removedAtIndex = _innerList.IndexOf(item);
            if (removedAtIndex < 0) return false;
            _innerList.RemoveAt(removedAtIndex);
        }
        RemoveAtNotifyHelper(removedAtIndex);
        EmitChangeEvent(new ListChangeEvent<TItem>.Removed(removedAtIndex, item));
        return true;
    }

    public void RemoveAt(int index)
    {
        TItem removedItem;
        lock (_writeLock)
        {
            removedItem = _innerList[index];
            _innerList.RemoveAt(index);
        }
        RemoveAtNotifyHelper(index);
        EmitChangeEvent(new ListChangeEvent<TItem>.Removed(index, removedItem));
    }

    private void RemoveAtNotifyHelper(int removedAtIndex)
    {
        List<DependencySlot> slotsToNotify;
        lock (_writeLock)
        {
            int newCount = _innerList.Count;
            slotsToNotify = new List<DependencySlot>();
            List<int> outOfBoundsKeys = new();
            foreach (var entry in _indexSlots)
            {
                if (entry.Key >= removedAtIndex) slotsToNotify.Add(entry.Value);
                if (entry.Key >= newCount) outOfBoundsKeys.Add(entry.Key);
            }
            foreach (int outOfBoundsKey in outOfBoundsKeys)
                _indexSlots.Remove(outOfBoundsKey);
        }
        foreach (DependencySlot indexSlot in slotsToNotify)
            indexSlot.NotifyAllSubscribers();
        _countSlot.NotifyAllSubscribers();
        _iterationSlot.NotifyAllSubscribers();
    }
}
