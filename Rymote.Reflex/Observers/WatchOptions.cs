namespace Rymote.Reflex.Observers;

public sealed record WatchOptions
{
    public bool Immediate { get; init; }
    public bool Deep { get; init; }
}
