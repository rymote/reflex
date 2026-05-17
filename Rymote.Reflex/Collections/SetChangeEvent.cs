namespace Rymote.Reflex.Collections;

/// <summary>Discriminated union describing a change that occurred on a <see cref="ReactiveSetWrapper{TItem}"/>.</summary>
/// <typeparam name="TItem">Element type of the set.</typeparam>
public abstract record SetChangeEvent<TItem> where TItem : notnull
{
    /// <summary>An item was added to the set.</summary>
    public sealed record Added(TItem Item) : SetChangeEvent<TItem>;

    /// <summary>An item was removed from the set.</summary>
    public sealed record Removed(TItem Item) : SetChangeEvent<TItem>;

    /// <summary>All items were removed via <c>Clear()</c>.</summary>
    public sealed record Cleared() : SetChangeEvent<TItem>;
}
