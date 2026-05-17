namespace Rymote.Reflex.Observers;

/// <summary>Options controlling the behaviour of a <c>Watch</c> subscription.</summary>
public sealed record WatchOptions
{
    /// <summary>When <see langword="true"/>, the handler is invoked immediately with the current value in addition to subsequent changes.</summary>
    public bool Immediate { get; init; }
}
