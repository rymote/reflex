namespace Rymote.Reflex.Collections;

/// <summary>Discriminated union describing a change that occurred on a <see cref="ReactiveDictionaryWrapper{TKey,TValue}"/>.</summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
public abstract record DictionaryChangeEvent<TKey, TValue> where TKey : notnull
{
    /// <summary>A new key/value pair was added.</summary>
    public sealed record Added(TKey Key, TValue Value) : DictionaryChangeEvent<TKey, TValue>;

    /// <summary>The entry for <see cref="Key"/> was removed.</summary>
    public sealed record Removed(TKey Key, TValue PreviousValue) : DictionaryChangeEvent<TKey, TValue>;

    /// <summary>The value for an existing <see cref="Key"/> was updated.</summary>
    public sealed record Replaced(TKey Key, TValue PreviousValue, TValue CurrentValue) : DictionaryChangeEvent<TKey, TValue>;

    /// <summary>All entries were removed via <c>Clear()</c>.</summary>
    public sealed record Cleared() : DictionaryChangeEvent<TKey, TValue>;
}
