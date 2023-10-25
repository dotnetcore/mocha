// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferConsumer<T> : IBufferConsumer<T>
{
    private readonly BufferConsumerOptions _options;
    private readonly MemoryBufferQueue<T> _queue;
    private volatile MemoryBufferPartition<T>[] _assignedPartitions = default!;
    private int _partitionIndex;
    private MemoryBufferPartition<T>? _partitionInConsumption;

    public MemoryBufferConsumer(BufferConsumerOptions options, MemoryBufferQueue<T> queue)
    {
        _options = options;
        _queue = queue;
    }

    public void AssignPartitions(MemoryBufferPartition<T>[] partitions)
    {
        _assignedPartitions = partitions;
    }

    public async IAsyncEnumerable<T> ConsumeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            _partitionInConsumption = SelectPartition();

            var valueTask = _partitionInConsumption.PullAsync(_options.GroupName);
            var item = valueTask.IsCompletedSuccessfully
                ? valueTask.Result
                : await valueTask.AsTask();

            if (_options.AutoCommit)
            {
                _partitionInConsumption.Commit(_options.GroupName);
            }

            yield return item;
        }
    }

    public ValueTask CommitAsync()
    {
        if (_options.AutoCommit)
        {
            throw new InvalidOperationException("Auto commit is enabled.");
        }

        var partition = _partitionInConsumption ??
                        throw new InvalidOperationException("No partition is in consumption.");

        partition.Commit(_options.GroupName);

        _partitionInConsumption = null;

        return ValueTask.CompletedTask;
    }

    public ValueTask CloseAsync()
    {
        _queue.RemoveConsumer(this);
        return ValueTask.CompletedTask;
    }

    private MemoryBufferPartition<T> SelectPartition()
    {
        var partitions = _assignedPartitions ??
                         throw new InvalidOperationException("No partition is assigned.");
        var index = (Interlocked.Increment(ref _partitionIndex) - 1) % partitions.Length;
        return partitions[index];
    }
}
