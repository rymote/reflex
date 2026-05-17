using System;
using System.Collections.Generic;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Primitives;

public sealed class Ref<TValue>
{
    private readonly DependencySlot _valueSlot = new();
    private readonly object _writeLock = new();
    private TValue _currentValue = default!;

    public Ref(TValue initialValue)
    {
        _currentValue = initialValue;
    }

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

    public ReadOnlyRef<TValue> AsReadOnly() => new(this);
}
