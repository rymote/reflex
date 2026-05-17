namespace Rymote.Reflex.Collections;

public abstract record DictionaryChangeEvent<TKey, TValue> where TKey : notnull
{
    public sealed record Added(TKey Key, TValue Value) : DictionaryChangeEvent<TKey, TValue>;
    public sealed record Removed(TKey Key, TValue PreviousValue) : DictionaryChangeEvent<TKey, TValue>;
    public sealed record Replaced(TKey Key, TValue PreviousValue, TValue CurrentValue) : DictionaryChangeEvent<TKey, TValue>;
    public sealed record Cleared() : DictionaryChangeEvent<TKey, TValue>;
}
