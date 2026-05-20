using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Rymote.Reflex.Core;

internal static class BatchStack
{
    private static readonly AsyncLocal<ImmutableStack<BatchFrame>?> _asyncFlowedFrameStack = new();

    internal static BatchFrame BeginNewFrame()
    {
        ImmutableStack<BatchFrame> currentStack =
            _asyncFlowedFrameStack.Value ?? ImmutableStack<BatchFrame>.Empty;
        BatchFrame newFrame = new();
        _asyncFlowedFrameStack.Value = currentStack.Push(newFrame);
        return newFrame;
    }

    internal static void RecordDirty(ReactiveEffect effect)
    {
        ImmutableStack<BatchFrame>? currentStack = _asyncFlowedFrameStack.Value;
        if (currentStack is not null && !currentStack.IsEmpty)
        {
            currentStack.Peek().AddDirtyEffect(effect);
            return;
        }

        effect.EnqueueOnScheduler();
    }

    internal static void PopAndMaybeFlush(BatchFrame disposedFrame, HashSet<ReactiveEffect> dirtyEffectsFromFrame)
    {
        ImmutableStack<BatchFrame>? currentStack = _asyncFlowedFrameStack.Value;
        if (currentStack is null || currentStack.IsEmpty)
            return;

        BatchFrame topFrame = currentStack.Peek();
        if (!ReferenceEquals(topFrame, disposedFrame))
            throw new System.InvalidOperationException("Batch frame pop mismatch.");

        ImmutableStack<BatchFrame> remainingStack = currentStack.Pop();
        _asyncFlowedFrameStack.Value = remainingStack;

        if (!remainingStack.IsEmpty)
        {
            BatchFrame parentFrame = remainingStack.Peek();
            foreach (ReactiveEffect dirtyEffect in dirtyEffectsFromFrame)
                parentFrame.AddDirtyEffect(dirtyEffect);
            return;
        }

        Rymote.Reflex.Scheduling.IReflexScheduler scheduler = ReflexConfiguration.ActiveOptions.Scheduler;
        scheduler.BeginScheduleSequence();
        try
        {
            foreach (ReactiveEffect dirtyEffect in dirtyEffectsFromFrame)
                dirtyEffect.EnqueueOnScheduler();
        }
        finally
        {
            scheduler.EndScheduleSequence();
        }
    }
}
