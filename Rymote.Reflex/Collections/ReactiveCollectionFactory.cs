using System.Collections.Generic;

namespace Rymote.Reflex.Collections;

internal static class ReactiveCollectionFactory
{
    internal static IList<TItem> WrapList<TItem>(IList<TItem> innerList) => new ReactiveListWrapper<TItem>(innerList);

    internal static IDictionary<TKey, TValue> WrapDictionary<TKey, TValue>(IDictionary<TKey, TValue> innerDictionary)
        where TKey : notnull
        => new ReactiveDictionaryWrapper<TKey, TValue>(innerDictionary);

    internal static ISet<TItem> WrapSet<TItem>(ISet<TItem> innerSet) where TItem : notnull
        => new ReactiveSetWrapper<TItem>(innerSet);
}
