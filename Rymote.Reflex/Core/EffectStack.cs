using System.Collections.Immutable;
using System.Threading;

namespace Rymote.Reflex.Core;

internal static class EffectStack
{
    private static readonly AsyncLocal<ImmutableStack<ReactiveEffect>?> _asyncFlowedStack = new();

    internal static ReactiveEffect? Current
    {
        get
        {
            ImmutableStack<ReactiveEffect>? currentStack = _asyncFlowedStack.Value;
            return currentStack is null || currentStack.IsEmpty ? null : currentStack.Peek();
        }
    }

    internal static void Push(ReactiveEffect effect)
    {
        ImmutableStack<ReactiveEffect> currentStack =
            _asyncFlowedStack.Value ?? ImmutableStack<ReactiveEffect>.Empty;
        _asyncFlowedStack.Value = currentStack.Push(effect);
    }

    internal static void Pop(ReactiveEffect expectedEffect)
    {
        ImmutableStack<ReactiveEffect>? currentStack = _asyncFlowedStack.Value;
        if (currentStack is null || currentStack.IsEmpty)
            throw new System.InvalidOperationException("EffectStack.Pop with empty stack.");

        ReactiveEffect topEffect = currentStack.Peek();
        if (!ReferenceEquals(topEffect, expectedEffect))
            throw new System.InvalidOperationException(
                "EffectStack.Pop popped an effect that was not on top.");

        _asyncFlowedStack.Value = currentStack.Pop();
    }
}
