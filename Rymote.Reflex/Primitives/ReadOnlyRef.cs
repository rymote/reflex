namespace Rymote.Reflex.Primitives;

/// <summary>A read-only view over a <see cref="Ref{TValue}"/>. Reads register reactive dependencies; the underlying value cannot be set through this wrapper.</summary>
/// <typeparam name="TValue">Type of the stored value.</typeparam>
public sealed class ReadOnlyRef<TValue>
{
    private readonly Ref<TValue> _underlyingRef;

    internal ReadOnlyRef(Ref<TValue> underlyingRef)
    {
        _underlyingRef = underlyingRef;
    }

    /// <summary>Gets the current value of the underlying ref. Reads inside an active effect register a dependency.</summary>
    public TValue Value => _underlyingRef.Value;
}
