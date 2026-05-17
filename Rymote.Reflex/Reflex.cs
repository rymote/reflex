using System;
using Rymote.Reflex.Core;
using Rymote.Reflex.Observers;

namespace Rymote.Reflex;

public static class Reflex
{
    public static ReflexOptions CurrentOptions => ReflexConfiguration.ActiveOptions;

    public static void Configure(Action<ReflexOptions> configureAction)
    {
        ArgumentNullException.ThrowIfNull(configureAction);
        ReflexConfiguration.Configure(configureAction);
    }

    public static void ConfigureForTests(Action<ReflexOptions> configureAction)
    {
        ArgumentNullException.ThrowIfNull(configureAction);
        ReflexConfiguration.ConfigureForTests(configureAction);
    }

    public static IDisposable Effect(Action effectCallback, string? debugName = null)
    {
        return EffectFactory.CreateAndRun(effectCallback, debugName);
    }

    public static IDisposable Effect(Action<IDisposable> effectCallbackWithSelf, string? debugName = null)
    {
        return EffectFactory.CreateAndRunWithSelf(effectCallbackWithSelf, debugName);
    }

    public static Primitives.Computed<TValue> Computed<TValue>(System.Func<TValue> evaluateFunction)
    {
        return new Primitives.Computed<TValue>(evaluateFunction);
    }

    public static System.IDisposable Batch()
    {
        return BatchStack.BeginNewFrame();
    }

    public static Core.EffectScope Scope()
    {
        return new Core.EffectScope();
    }

    public static System.IDisposable Watch<TValue>(
        Primitives.Ref<TValue> sourceRef,
        System.Action<TValue, TValue> handler,
        Observers.WatchOptions? watchOptions = null)
    {
        return Observers.WatchFactory.WatchRef(sourceRef, handler, watchOptions);
    }

    public static System.IDisposable Watch<TValue>(
        Primitives.ReadOnlyRef<TValue> sourceRef,
        System.Action<TValue, TValue> handler,
        Observers.WatchOptions? watchOptions = null)
    {
        return Observers.WatchFactory.WatchReadOnlyRef(sourceRef, handler, watchOptions);
    }

    public static System.IDisposable Watch<TValue>(
        Primitives.Computed<TValue> sourceComputed,
        System.Action<TValue, TValue> handler,
        Observers.WatchOptions? watchOptions = null)
    {
        return Observers.WatchFactory.WatchComputed(sourceComputed, handler, watchOptions);
    }

    public static System.IDisposable Watch<TValue>(
        System.Func<TValue> selectorFunction,
        System.Action<TValue, TValue> handler,
        Observers.WatchOptions? watchOptions = null)
    {
        return Observers.WatchFactory.WatchSelector(selectorFunction, handler, watchOptions);
    }

    public static System.IDisposable WatchSyncEffect(System.Action effectCallback, string? debugName = null)
    {
        return Observers.WatchSyncEffectFactory.CreateAndRun(effectCallback, debugName);
    }

    public static System.IDisposable WatchPostEffect(System.Action effectCallback, string? debugName = null)
    {
        return Observers.WatchPostEffectFactory.CreateAndRun(effectCallback, debugName);
    }

    public static System.Collections.Generic.IList<TItem> Reactive<TItem>(System.Collections.Generic.IList<TItem> innerList)
    {
        return Collections.ReactiveCollectionFactory.WrapList(innerList);
    }

    public static System.Collections.Generic.IDictionary<TKey, TValue> Reactive<TKey, TValue>(
        System.Collections.Generic.IDictionary<TKey, TValue> innerDictionary)
        where TKey : notnull
    {
        return Collections.ReactiveCollectionFactory.WrapDictionary(innerDictionary);
    }

    public static System.Collections.Generic.ISet<TItem> Reactive<TItem>(System.Collections.Generic.ISet<TItem> innerSet)
        where TItem : notnull
    {
        return Collections.ReactiveCollectionFactory.WrapSet(innerSet);
    }

    public static bool IsRef(object? candidate)
    {
        if (candidate is null) return false;
        System.Type candidateType = candidate.GetType();
        return candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == typeof(Primitives.Ref<>);
    }

    public static bool IsReadOnlyRef(object? candidate)
    {
        if (candidate is null) return false;
        System.Type candidateType = candidate.GetType();
        return candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == typeof(Primitives.ReadOnlyRef<>);
    }

    public static bool IsComputed(object? candidate)
    {
        if (candidate is null) return false;
        System.Type candidateType = candidate.GetType();
        return candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == typeof(Primitives.Computed<>);
    }

    public static bool IsReactive(object? candidate)
    {
        return candidate is Utilities.IReactive;
    }

    public static TValue Unref<TValue>(object? candidate)
    {
        if (candidate is Primitives.Ref<TValue> sourceRef) return sourceRef.Value;
        if (candidate is Primitives.ReadOnlyRef<TValue> readOnlyRef) return readOnlyRef.Value;
        if (candidate is Primitives.Computed<TValue> sourceComputed) return sourceComputed.Value;
        if (candidate is TValue rawValue) return rawValue;
        throw new System.InvalidCastException(
            $"Cannot unref a value of type {candidate?.GetType().Name ?? "null"} as {typeof(TValue).Name}.");
    }

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
