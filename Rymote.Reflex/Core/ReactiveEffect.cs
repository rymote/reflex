using System;
using System.Collections.Generic;
using System.Threading;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Core;

public sealed class ReactiveEffect
{
    private readonly Action<ReactiveEffect> _userCallback;
    private readonly string? _debugName;
    private readonly List<DependencySlot> _trackedSlots = new();
    private readonly object _runLock = new();
    private readonly bool _isSynchronous;
    private readonly bool _isPostTick;
    private int _pendingFlag;
    private int _disposedFlag;
    private int _consecutiveFailureCount;
    private int _runsThisTick;

    internal ReactiveEffect(Action<ReactiveEffect> userCallback, string? debugName,
        bool isSynchronous = false, bool isPostTick = false)
    {
        _userCallback = userCallback;
        _debugName = debugName;
        _isSynchronous = isSynchronous;
        _isPostTick = isPostTick;
    }

    public string? DebugName => _debugName;

    internal bool IsSynchronous => _isSynchronous;
    internal bool IsPostTick => _isPostTick;

    internal bool IsDisposed => Volatile.Read(ref _disposedFlag) == 1;

    internal void RunInitially()
    {
        Run();
    }

    internal void ScheduleRerun()
    {
        if (IsDisposed) return;

        if (_isSynchronous)
        {
            Run();
            return;
        }

        BatchStack.RecordDirty(this);
    }

    internal void EnqueueOnScheduler()
    {
        if (IsDisposed)
            return;

        ReflexConfiguration.ActiveOptions.Scheduler.Schedule(this);
    }

    internal bool MarkPending()
    {
        return Interlocked.CompareExchange(ref _pendingFlag, 1, 0) == 0;
    }

    internal void ClearPending()
    {
        Interlocked.Exchange(ref _pendingFlag, 0);
    }

    internal void TrackSlotForCleanup(DependencySlot slot)
    {
        _trackedSlots.Add(slot);
    }

    internal void Run()
    {
        if (IsDisposed)
            return;

        lock (_runLock)
        {
            if (IsDisposed)
                return;

            int reentrantBudget = ReflexConfiguration.ActiveOptions.MaxReentrantRunsPerTick;
            if (Interlocked.Increment(ref _runsThisTick) > reentrantBudget)
            {
                DisposeAndDetach();
                ReflexConfiguration.ActiveOptions.OnEffectException(
                    new InvalidOperationException(
                        $"Effect '{_debugName ?? "<unnamed>"}' exceeded " +
                        $"MaxReentrantRunsPerTick ({reentrantBudget}) and was disabled."),
                    new EffectExceptionInfo(_debugName, _consecutiveFailureCount));
                return;
            }

            DetachFromTrackedSlots();

            EffectStack.Push(this);
            try
            {
                _userCallback(this);
                _consecutiveFailureCount = 0;
            }
            catch (Exception raisedException)
            {
                int failureCount = Interlocked.Increment(ref _consecutiveFailureCount);
                ReflexConfiguration.ActiveOptions.OnEffectException(
                    raisedException,
                    new EffectExceptionInfo(_debugName, failureCount));
            }
            finally
            {
                EffectStack.Pop(this);
            }

            if (Volatile.Read(ref _pendingFlag) == 0)
                Interlocked.Exchange(ref _runsThisTick, 0);
        }
    }

    internal void DisposeAndDetach()
    {
        if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0)
            return;

        DetachFromTrackedSlots();
    }

    private void DetachFromTrackedSlots()
    {
        foreach (DependencySlot slot in _trackedSlots)
            slot.RemoveSubscriber(this);
        _trackedSlots.Clear();
    }
}
