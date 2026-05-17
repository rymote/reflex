namespace Rymote.Reflex.Core;

/// <summary>Contextual information passed to <see cref="ReflexOptions.OnEffectException"/> when an effect throws.</summary>
/// <param name="DebugName">The optional debug name supplied when the effect was created.</param>
/// <param name="ConsecutiveFailures">The number of consecutive times this effect has thrown without a successful run in between.</param>
public sealed record EffectExceptionInfo(string? DebugName, int ConsecutiveFailures);
