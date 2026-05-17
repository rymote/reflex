namespace Rymote.Reflex.Collections;

public abstract record SetChangeEvent<TItem> where TItem : notnull
{
    public sealed record Added(TItem Item) : SetChangeEvent<TItem>;
    public sealed record Removed(TItem Item) : SetChangeEvent<TItem>;
    public sealed record Cleared() : SetChangeEvent<TItem>;
}
