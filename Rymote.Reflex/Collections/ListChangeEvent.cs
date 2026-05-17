namespace Rymote.Reflex.Collections;

/// <summary>Discriminated union describing a change that occurred on a <see cref="ReactiveListWrapper{TItem}"/>.</summary>
/// <typeparam name="TItem">Element type of the list.</typeparam>
public abstract record ListChangeEvent<TItem>
{
    /// <summary>An item was appended or inserted at <see cref="Index"/>.</summary>
    public sealed record Added(int Index, TItem Item) : ListChangeEvent<TItem>;

    /// <summary>An item was removed from <see cref="Index"/>.</summary>
    public sealed record Removed(int Index, TItem Item) : ListChangeEvent<TItem>;

    /// <summary>The item at <see cref="Index"/> was replaced.</summary>
    public sealed record Replaced(int Index, TItem PreviousItem, TItem CurrentItem) : ListChangeEvent<TItem>;

    /// <summary>All items were removed via <c>Clear()</c>.</summary>
    public sealed record Cleared() : ListChangeEvent<TItem>;
}
