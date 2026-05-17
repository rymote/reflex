using System;
using System.Collections.Generic;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Primitives;

/// <summary>Wraps a single mutable value, exposing it as a tracked reactive source.</summary>
/// <typeparam name="TValue">Type of the stored value.</typeparam>
public sealed class Ref<TValue>
{
    private readonly DependencySlot _valueSlot = new();
    private readonly object _writeLock = new();
    private TValue _currentValue = default!;

    /// <summary>Initializes a new <see cref="Ref{TValue}"/> with the given initial value.</summary>
    /// <param name="initialValue">The starting value.</param>
    public Ref(TValue initialValue)
    {
        _currentValue = initialValue;
    }

    /// <summary>Gets or sets the current value. Reads inside an active effect register a dependency; writes notify subscribed effects.</summary>
    public TValue Value
    {
        get
        {
            _valueSlot.RegisterCurrentEffectAsDependent();
            lock (_writeLock)
                return _currentValue;
        }
        set
        {
            bool valueChanged;
            lock (_writeLock)
            {
                valueChanged = !EqualityComparer<TValue>.Default.Equals(_currentValue, value);
                if (valueChanged)
                    _currentValue = value;
            }

            if (valueChanged)
                _valueSlot.NotifyAllSubscribers();
        }
    }

    /// <summary>Applies a transform function to the current value atomically and notifies subscribers if the result differs.</summary>
    /// <param name="transformFunction">A pure function that receives the current value and returns the new value.</param>
    public void Update(Func<TValue, TValue> transformFunction)
    {
        ArgumentNullException.ThrowIfNull(transformFunction);

        bool valueChanged;
        lock (_writeLock)
        {
            TValue newValue = transformFunction(_currentValue);
            valueChanged = !EqualityComparer<TValue>.Default.Equals(_currentValue, newValue);
            if (valueChanged)
                _currentValue = newValue;
        }

        if (valueChanged)
            _valueSlot.NotifyAllSubscribers();
    }

    /// <summary>Returns a read-only view of this ref that exposes <see cref="Value"/> but disallows writes.</summary>
    public ReadOnlyRef<TValue> AsReadOnly() => new(this);
}
