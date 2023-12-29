// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferConsumer<T> : IBufferConsumer<T>
{
    private readonly BufferConsumerOptions _options;
    private volatile MemoryBufferPartition<T>[] _assignedPartitions;

    private int _partitionIndex;
    private MemoryBufferPartition<T>? _partitionBeingConsumed;

    private volatile int _pendingDataVersion;
    private readonly PendingDataValueTaskSource<MemoryBufferPartition<T>> _pendingDataValueTaskSource;
    private readonly ReaderWriterLockSlim _pendingDataLock;

    public MemoryBufferConsumer(BufferConsumerOptions options)
    {
        _options = options;
        _assignedPartitions = Array.Empty<MemoryBufferPartition<T>>();
        _pendingDataValueTaskSource = new PendingDataValueTaskSource<MemoryBufferPartition<T>>();
        _pendingDataVersion = 0;
        _pendingDataLock = new ReaderWriterLockSlim();
    }

    public string TopicName => _options.TopicName;

    public string GroupName => _options.GroupName;

    public void AssignPartitions(params MemoryBufferPartition<T>[] partitions)
    {
        _assignedPartitions = partitions;
        foreach (var partition in partitions)
        {
            partition.RegisterConsumer(this);
        }
    }

    public async IAsyncEnumerable<IEnumerable<T>> ConsumeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_assignedPartitions.Length == 0)
        {
            throw new InvalidOperationException("No partition is assigned.");
        }

        while (true)
        {
            var pendingDataVersion = _pendingDataVersion;

            var partition = SelectPartition();

            var batchSize = _options.BatchSize;

            if (TryPull(partition, batchSize, out var items))
            {
                yield return items;
                continue;
            }

            // Try to pull from other partitions
            IEnumerable<T> itemsFromOtherPartition = default!;
            var hasItemFromOtherPartition = false;

            foreach (var t in _assignedPartitions)
            {
                partition = t;

                if (partition == _partitionBeingConsumed)
                {
                    continue;
                }

                if (TryPull(partition, batchSize, out items))
                {
                    itemsFromOtherPartition = items;
                    hasItemFromOtherPartition = true;
                    break;
                }
            }

            if (hasItemFromOtherPartition)
            {
                yield return itemsFromOtherPartition;
                continue;
            }

            try
            {
                _pendingDataLock.EnterWriteLock();

                // Check if the pending data version is changed,
                // if so, it means that the pending data is already to be consumed.
                if (_pendingDataVersion != pendingDataVersion)
                {
                    continue;
                }

                // Mark the consumer is waiting for data.
                _pendingDataValueTaskSource.Reset();
            }
            finally
            {
                _pendingDataLock.ExitWriteLock();
            }

            var pendingDataTask = _pendingDataValueTaskSource.ValueTask;

            var partitionWithNewData = pendingDataTask.IsCompletedSuccessfully
                ? pendingDataTask.Result
                : await pendingDataTask;

            if (TryPull(partitionWithNewData, batchSize, out items))
            {
                yield return items;
            }
        }
    }

    public ValueTask CommitAsync()
    {
        if (_options.AutoCommit)
        {
            throw new InvalidOperationException("Auto commit is enabled.");
        }

        var partition = _partitionBeingConsumed ??
                        throw new InvalidOperationException("No partition is in consumption.");

        partition.Commit(_options.GroupName);

        _partitionBeingConsumed = null;

        return ValueTask.CompletedTask;
    }

    public void NotifyNewDataAvailable(MemoryBufferPartition<T> partition)
    {
        Interlocked.Increment(ref _pendingDataVersion);

        _pendingDataLock.EnterUpgradeableReadLock();
        try
        {
            if (!_pendingDataValueTaskSource.IsWaiting)
            {
                return;
            }

            _pendingDataLock.EnterWriteLock();
            try
            {
                if (!_pendingDataValueTaskSource.IsWaiting)
                {
                    return;
                }

                _pendingDataValueTaskSource.SetResult(partition);
            }
            finally
            {
                _pendingDataLock.ExitWriteLock();
            }
        }
        finally
        {
            _pendingDataLock.ExitUpgradeableReadLock();
        }
    }

    private bool TryPull(MemoryBufferPartition<T> partition, int batchSize,
        [NotNullWhen(true)] out IEnumerable<T>? items)
    {
        _partitionBeingConsumed = partition;
        var dataAvailable = partition.TryPull(_options.GroupName, batchSize, out items);

        if (dataAvailable)
        {
            AutoCommitIfEnabled(partition);
        }

        return dataAvailable;
    }

    private void AutoCommitIfEnabled(MemoryBufferPartition<T> partition)
    {
        if (_options.AutoCommit)
        {
            partition.Commit(_options.GroupName);
        }
    }

    private MemoryBufferPartition<T> SelectPartition()
    {
        var partitions = _assignedPartitions;

        if (partitions.Length == 0)
        {
            throw new InvalidOperationException("No partition is assigned.");
        }

        var index = _partitionIndex++ % partitions.Length;
        return partitions[index];
    }
}
