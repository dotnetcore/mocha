// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Threading.Tasks.Sources;

namespace Mocha.Core.Buffer.Memory;

internal sealed class PendingDataValueTaskSource<T> : IValueTaskSource<T>
{
    private ManualResetValueTaskSourceCore<T> _core = new() { RunContinuationsAsynchronously = true };

    // Default value for ValueTask is a completed task.
    private ValueTask<T> _valueTask;
    private volatile bool _isWaiting;

    public ValueTask<T> ValueTask => _valueTask;

    public bool IsWaiting => _isWaiting;

    public void Reset()
    {
        _isWaiting = true;
        _core.Reset();
        _valueTask = new ValueTask<T>(this, _core.Version);
    }

    public void SetResult(T result)
    {
        _isWaiting = false;
        _core.SetResult(result);
    }

    public T GetResult(short token) => _core.GetResult(token);

    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

    public void OnCompleted(
        Action<object?> continuation,
        object? state,
        short token,
        ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);
}
