namespace Rymote.Reflex.Primitives;

public sealed class ReadOnlyRef<TValue>
{
    private readonly Ref<TValue> _underlyingRef;

    internal ReadOnlyRef(Ref<TValue> underlyingRef)
    {
        _underlyingRef = underlyingRef;
    }

    public TValue Value => _underlyingRef.Value;
}
