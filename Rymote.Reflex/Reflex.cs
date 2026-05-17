using System;
using Rymote.Reflex.Core;
using Rymote.Reflex.Observers;

namespace Rymote.Reflex;

/// <summary>Static facade providing the primary API surface for creating and managing reactive primitives.</summary>
public static class Reflex
{
    /// <summary>Gets the current global configuration options.</summary>
    public static ReflexOptions CurrentOptions => ReflexConfiguration.ActiveOptions;

    /// <summary>Applies a configuration action to the global <see cref="ReflexOptions"/>.</summary>
    /// <param name="configureAction">Delegate that mutates the options instance.</param>
    public static void Configure(Action<ReflexOptions> configureAction)
    {
        ArgumentNullException.ThrowIfNull(configureAction);
        ReflexConfiguration.Configure(configureAction);
    }

    /// <summary>Applies a test-scoped configuration action that is isolated per async context.
    /// Intended for use inside unit tests to avoid polluting the global configuration.</summary>
    /// <param name="configureAction">Delegate that mutates the options instance.</param>
    public static void ConfigureForTests(Action<ReflexOptions> configureAction)
    {
        ArgumentNullException.ThrowIfNull(configureAction);
        ReflexConfiguration.ConfigureForTests(configureAction);
    }

    /// <summary>Creates and immediately runs a reactive effect. The effect re-runs whenever any reactive source read inside it changes.</summary>
    /// <param name="effectCallback">The body of the effect.</param>
    /// <param name="debugName">Optional name shown in diagnostics and error messages.</param>
    /// <returns>A disposable that, when disposed, stops the effect from re-running.</returns>
    public static IDisposable Effect(Action effectCallback, string? debugName = null)
    {
        return EffectFactory.CreateAndRun(effectCallback, debugName);
    }

    /// <summary>Creates and immediately runs a reactive effect. The <paramref name="effectCallbackWithSelf"/> receives its own
    /// subscription handle, allowing self-disposal from within the effect body.</summary>
    /// <param name="effectCallbackWithSelf">The body of the effect; receives its own <see cref="IDisposable"/> subscription.</param>
    /// <param name="debugName">Optional name shown in diagnostics and error messages.</param>
    /// <returns>A disposable that, when disposed, stops the effect from re-running.</returns>
    public static IDisposable Effect(Action<IDisposable> effectCallbackWithSelf, string? debugName = null)
    {
        return EffectFactory.CreateAndRunWithSelf(effectCallbackWithSelf, debugName);
    }

    /// <summary>Creates a lazily-evaluated reactive computed value that re-evaluates when its dependencies change.</summary>
    /// <typeparam name="TValue">Type of the computed result.</typeparam>
    /// <param name="evaluateFunction">Pure function whose reactive reads form the dependency graph.</param>
    /// <returns>A <see cref="Primitives.Computed{TValue}"/> that caches the result until invalidated.</returns>
    public static Primitives.Computed<TValue> Computed<TValue>(System.Func<TValue> evaluateFunction)
    {
        return new Primitives.Computed<TValue>(evaluateFunction);
    }

    /// <summary>Begins a batch. All effect notifications triggered within the batch are deferred until the returned handle is disposed.</summary>
    /// <returns>A disposable whose disposal ends the batch and flushes deferred notifications.</returns>
    public static System.IDisposable Batch()
    {
        return BatchStack.BeginNewFrame();
    }

    /// <summary>Creates a new <see cref="Core.EffectScope"/> that owns a set of effects and disposes them together.</summary>
    /// <returns>A new, empty <see cref="Core.EffectScope"/>.</returns>
    public static Core.EffectScope Scope()
    {
        return new Core.EffectScope();
    }

    /// <summary>Watches a <see cref="Primitives.Ref{TValue}"/> for changes and calls <paramref name="handler"/> with the old and new values.</summary>
    /// <typeparam name="TValue">Type of the watched value.</typeparam>
    /// <param name="sourceRef">The ref to watch.</param>
    /// <param name="handler">Callback invoked with <c>(oldValue, newValue)</c> on each change.</param>
    /// <param name="watchOptions">Optional options; see <see cref="Observers.WatchOptions"/>.</param>
    /// <returns>A disposable that stops the watch when disposed.</returns>
    public static System.IDisposable Watch<TValue>(
        Primitives.Ref<TValue> sourceRef,
        System.Action<TValue, TValue> handler,
        Observers.WatchOptions? watchOptions = null)
    {
        return Observers.WatchFactory.WatchRef(sourceRef, handler, watchOptions);
    }

    /// <summary>Watches a <see cref="Primitives.ReadOnlyRef{TValue}"/> for changes and calls <paramref name="handler"/> with the old and new values.</summary>
    /// <typeparam name="TValue">Type of the watched value.</typeparam>
    /// <param name="sourceRef">The read-only ref to watch.</param>
    /// <param name="handler">Callback invoked with <c>(oldValue, newValue)</c> on each change.</param>
    /// <param name="watchOptions">Optional options; see <see cref="Observers.WatchOptions"/>.</param>
    /// <returns>A disposable that stops the watch when disposed.</returns>
    public static System.IDisposable Watch<TValue>(
        Primitives.ReadOnlyRef<TValue> sourceRef,
        System.Action<TValue, TValue> handler,
        Observers.WatchOptions? watchOptions = null)
    {
        return Observers.WatchFactory.WatchReadOnlyRef(sourceRef, handler, watchOptions);
    }

    /// <summary>Watches a <see cref="Primitives.Computed{TValue}"/> for changes and calls <paramref name="handler"/> with the old and new values.</summary>
    /// <typeparam name="TValue">Type of the computed result.</typeparam>
    /// <param name="sourceComputed">The computed to watch.</param>
    /// <param name="handler">Callback invoked with <c>(oldValue, newValue)</c> on each change.</param>
    /// <param name="watchOptions">Optional options; see <see cref="Observers.WatchOptions"/>.</param>
    /// <returns>A disposable that stops the watch when disposed.</returns>
    public static System.IDisposable Watch<TValue>(
        Primitives.Computed<TValue> sourceComputed,
        System.Action<TValue, TValue> handler,
        Observers.WatchOptions? watchOptions = null)
    {
        return Observers.WatchFactory.WatchComputed(sourceComputed, handler, watchOptions);
    }

    /// <summary>Watches an auto-tracked selector function for changes and calls <paramref name="handler"/> with the old and new values.
    /// Any reactive sources read inside <paramref name="selectorFunction"/> become dependencies.</summary>
    /// <typeparam name="TValue">Type returned by the selector.</typeparam>
    /// <param name="selectorFunction">Tracked selector whose reactive reads form the dependency graph.</param>
    /// <param name="handler">Callback invoked with <c>(oldValue, newValue)</c> on each change.</param>
    /// <param name="watchOptions">Optional options; see <see cref="Observers.WatchOptions"/>.</param>
    /// <returns>A disposable that stops the watch when disposed.</returns>
    public static System.IDisposable Watch<TValue>(
        System.Func<TValue> selectorFunction,
        System.Action<TValue, TValue> handler,
        Observers.WatchOptions? watchOptions = null)
    {
        return Observers.WatchFactory.WatchSelector(selectorFunction, handler, watchOptions);
    }

    /// <summary>Creates a synchronous watch effect that runs on the writer thread immediately when a dependency changes, bypassing the scheduler.
    /// Use as an escape hatch when ordering matters more than batching.</summary>
    /// <param name="effectCallback">The effect body.</param>
    /// <param name="debugName">Optional name shown in diagnostics and error messages.</param>
    /// <returns>A disposable that stops the effect when disposed.</returns>
    public static System.IDisposable WatchSyncEffect(System.Action effectCallback, string? debugName = null)
    {
        return Observers.WatchSyncEffectFactory.CreateAndRun(effectCallback, debugName);
    }

    /// <summary>Creates a post-tick watch effect that runs after all normal effects in the current scheduler flush have completed.</summary>
    /// <param name="effectCallback">The effect body.</param>
    /// <param name="debugName">Optional name shown in diagnostics and error messages.</param>
    /// <returns>A disposable that stops the effect when disposed.</returns>
    public static System.IDisposable WatchPostEffect(System.Action effectCallback, string? debugName = null)
    {
        return Observers.WatchPostEffectFactory.CreateAndRun(effectCallback, debugName);
    }

    /// <summary>Wraps a BCL <see cref="System.Collections.Generic.IList{T}"/> in a reactive proxy that notifies effects on mutation.</summary>
    /// <typeparam name="TItem">Element type.</typeparam>
    /// <param name="innerList">The underlying list to wrap.</param>
    /// <returns>A reactive <see cref="System.Collections.Generic.IList{T}"/> whose reads and writes are tracked.</returns>
    public static System.Collections.Generic.IList<TItem> Reactive<TItem>(System.Collections.Generic.IList<TItem> innerList)
    {
        return Collections.ReactiveCollectionFactory.WrapList(innerList);
    }

    /// <summary>Wraps a BCL <see cref="System.Collections.Generic.IDictionary{TKey,TValue}"/> in a reactive proxy that notifies effects on mutation.</summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="innerDictionary">The underlying dictionary to wrap.</param>
    /// <returns>A reactive <see cref="System.Collections.Generic.IDictionary{TKey,TValue}"/> whose reads and writes are tracked per key.</returns>
    public static System.Collections.Generic.IDictionary<TKey, TValue> Reactive<TKey, TValue>(
        System.Collections.Generic.IDictionary<TKey, TValue> innerDictionary)
        where TKey : notnull
    {
        return Collections.ReactiveCollectionFactory.WrapDictionary(innerDictionary);
    }

    /// <summary>Wraps a BCL <see cref="System.Collections.Generic.ISet{T}"/> in a reactive proxy that notifies effects on mutation.</summary>
    /// <typeparam name="TItem">Element type.</typeparam>
    /// <param name="innerSet">The underlying set to wrap.</param>
    /// <returns>A reactive <see cref="System.Collections.Generic.ISet{T}"/> whose reads and writes are tracked per value.</returns>
    public static System.Collections.Generic.ISet<TItem> Reactive<TItem>(System.Collections.Generic.ISet<TItem> innerSet)
        where TItem : notnull
    {
        return Collections.ReactiveCollectionFactory.WrapSet(innerSet);
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="candidate"/> is a <see cref="Primitives.Ref{TValue}"/> instance.</summary>
    /// <param name="candidate">The object to test.</param>
    public static bool IsRef(object? candidate)
    {
        if (candidate is null) return false;
        System.Type candidateType = candidate.GetType();
        return candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == typeof(Primitives.Ref<>);
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="candidate"/> is a <see cref="Primitives.ReadOnlyRef{TValue}"/> instance.</summary>
    /// <param name="candidate">The object to test.</param>
    public static bool IsReadOnlyRef(object? candidate)
    {
        if (candidate is null) return false;
        System.Type candidateType = candidate.GetType();
        return candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == typeof(Primitives.ReadOnlyRef<>);
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="candidate"/> is a <see cref="Primitives.Computed{TValue}"/> instance.</summary>
    /// <param name="candidate">The object to test.</param>
    public static bool IsComputed(object? candidate)
    {
        if (candidate is null) return false;
        System.Type candidateType = candidate.GetType();
        return candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == typeof(Primitives.Computed<>);
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="candidate"/> implements <see cref="Utilities.IReactive"/>.</summary>
    /// <param name="candidate">The object to test.</param>
    public static bool IsReactive(object? candidate)
    {
        return candidate is Utilities.IReactive;
    }

    /// <summary>Unwraps a reactive primitive to its underlying value, or returns the raw value if <paramref name="candidate"/> is not a reactive wrapper.</summary>
    /// <typeparam name="TValue">The expected value type.</typeparam>
    /// <param name="candidate">A <see cref="Primitives.Ref{TValue}"/>, <see cref="Primitives.ReadOnlyRef{TValue}"/>, <see cref="Primitives.Computed{TValue}"/>, or a raw <typeparamref name="TValue"/>.</param>
    /// <returns>The underlying value.</returns>
    /// <exception cref="System.InvalidCastException">Thrown when the candidate cannot be unwrapped to <typeparamref name="TValue"/>.</exception>
    public static TValue Unref<TValue>(object? candidate)
    {
        if (candidate is Primitives.Ref<TValue> sourceRef) return sourceRef.Value;
        if (candidate is Primitives.ReadOnlyRef<TValue> readOnlyRef) return readOnlyRef.Value;
        if (candidate is Primitives.Computed<TValue> sourceComputed) return sourceComputed.Value;
        if (candidate is TValue rawValue) return rawValue;
        throw new System.InvalidCastException(
            $"Cannot unref a value of type {candidate?.GetType().Name ?? "null"} as {typeof(TValue).Name}.");
    }

    /// <summary>Projects a property from a reactive source into a standalone <see cref="Primitives.Ref{TProperty}"/> that stays synchronized via an internal effect.</summary>
    /// <typeparam name="TSource">A type implementing <see cref="Utilities.IReactive"/>.</typeparam>
    /// <typeparam name="TProperty">Type of the projected property.</typeparam>
    /// <param name="source">The reactive source object.</param>
    /// <param name="propertySelector">Selector that reads the target property from <paramref name="source"/>.</param>
    /// <returns>A <see cref="Primitives.Ref{TProperty}"/> that mirrors the selected property and updates when it changes.</returns>
    public static Primitives.Ref<TProperty> ToRef<TSource, TProperty>(
        TSource source,
        System.Func<TSource, TProperty> propertySelector)
        where TSource : Utilities.IReactive
    {
        System.ArgumentNullException.ThrowIfNull(source);
        System.ArgumentNullException.ThrowIfNull(propertySelector);
        Primitives.Ref<TProperty> projectedRef = new(propertySelector(source));
        Effect(() => projectedRef.Value = propertySelector(source));
        return projectedRef;
    }
}
