using System;
using System.Threading;

namespace Rymote.Reflex.Core;

internal static class ReflexConfiguration
{
    private static ReflexOptions _activeOptions = new();
    private static int _configureCalledOnce;

    internal static ReflexOptions ActiveOptions => _activeOptions;

    internal static void Configure(Action<ReflexOptions> configureAction)
    {
        if (Interlocked.CompareExchange(ref _configureCalledOnce, 1, 0) != 0)
            throw new InvalidOperationException(
                "Rymote.Reflex.Reflex.Configure has already been called. " +
                "Use Rymote.Reflex.Reflex.ConfigureForTests to replace options inside tests.");

        ReflexOptions replacementOptions = new();
        configureAction(replacementOptions);
        _activeOptions = replacementOptions;
    }

    internal static void ConfigureForTests(Action<ReflexOptions> configureAction)
    {
        ReflexOptions replacementOptions = new();
        configureAction(replacementOptions);
        _activeOptions = replacementOptions;
        Interlocked.Exchange(ref _configureCalledOnce, 0);
    }
}
