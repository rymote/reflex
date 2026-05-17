namespace Rymote.Reflex.Collections;

public abstract record ListChangeEvent<TItem>
{
    public sealed record Added(int Index, TItem Item) : ListChangeEvent<TItem>;
    public sealed record Removed(int Index, TItem Item) : ListChangeEvent<TItem>;
    public sealed record Replaced(int Index, TItem PreviousItem, TItem CurrentItem) : ListChangeEvent<TItem>;
    public sealed record Cleared() : ListChangeEvent<TItem>;
}
