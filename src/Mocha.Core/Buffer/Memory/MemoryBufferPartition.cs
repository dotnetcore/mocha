// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mocha.Core.Buffer.Memory;

[DebuggerDisplay("PartitionId = {PartitionId}, Capacity = {Capacity}, Count = {Count}")]
[DebuggerTypeProxy(typeof(MemoryBufferPartition<>.DebugView))]
internal sealed class MemoryBufferPartition<T>
{
    // internal for testing
    internal static int SegmentLength = 1024;

    private static int _idIncreasement;

    private volatile MemoryBufferSegment<T> _head;
    private volatile MemoryBufferSegment<T> _tail;

    // At most one consumer per group can consume the same partition at the same time,
    private readonly ConcurrentDictionary<string /* group name */, Reader> _consumerReaders;
    private readonly HashSet<MemoryBufferConsumer<T>> _consumers;

    private readonly object _createSegmentLock;

    public MemoryBufferPartition()
    {
        PartitionId = _idIncreasement++;
        _head = _tail = new MemoryBufferSegment<T>(SegmentLength, default);
        _consumerReaders = new ConcurrentDictionary<string, Reader>();
        _consumers = new HashSet<MemoryBufferConsumer<T>>();

        _createSegmentLock = new object();
    }

    public int PartitionId { get; }

    public ulong Capacity => (ulong)(_tail.EndOffset - _head.StartOffset + 1);

    public ulong Count
    {
        get
        {
            var freeCount = (ulong)(_tail.Capacity - _tail.Count);
            return Capacity - freeCount;
        }
    }

    public void RegisterConsumer(MemoryBufferConsumer<T> consumer) => _consumers.Add(consumer);

    public void Enqueue(T item)
    {
        while (true)
        {
            var tail = _tail;

            if (tail.TryEnqueue(item))
            {
                foreach (var consumer in _consumers)
                {
                    consumer.NotifyNewDataAvailable(this);
                }

                return;
            }

            lock (_createSegmentLock)
            {
                if (_tail != tail)
                {
                    // tail has been changed, retry
                    continue;
                }

                var newSegmentStartOffset = tail.EndOffset + 1;
                var newSegment = TryRecycleSegment(newSegmentStartOffset, out var recycledSegment)
                    ? recycledSegment
                    : new MemoryBufferSegment<T>(SegmentLength, newSegmentStartOffset);
                tail.NextSegment = newSegment;
                _tail = newSegment;
            }
        }
    }

    public bool TryPull(string groupName, [NotNullWhen(true)] out T item)
    {
        var reader = _consumerReaders.GetOrAdd(
            groupName,
            _ => new Reader(_head, _head.StartOffset));

        return reader.TryRead(out item);
    }

    public void Commit(string groupName)
    {
        if (!_consumerReaders.TryGetValue(groupName, out var reader))
        {
            throw new InvalidOperationException("Specified group name not found.");
        }

        reader.MoveNext();
    }

    private bool TryRecycleSegment(
        MemoryBufferPartitionOffset newSegmentStartOffset,
        [NotNullWhen(true)] out MemoryBufferSegment<T>? recycledSegment)
    {
        recycledSegment = null;

        if (_head == _tail)
        {
            return false;
        }

        var minConsumerPendingOffset = MinConsumerPendingOffset();

        MemoryBufferSegment<T>? recyclableSegment = null;
        for (var segment = _head; segment != _tail; segment = segment.NextSegment!)
        {
            var wholeSegmentConsumed = segment.EndOffset < minConsumerPendingOffset;
            if (wholeSegmentConsumed)
            {
                recyclableSegment = segment;
            }
        }

        if (recyclableSegment == null)
        {
            return false;
        }

        recycledSegment = recyclableSegment.RecycleSlots(newSegmentStartOffset);

        _head = recyclableSegment.NextSegment!;
        _tail = recycledSegment;

        return true;
    }

    private MemoryBufferPartitionOffset MinConsumerPendingOffset()
    {
        MemoryBufferPartitionOffset? minPendingOffset = null;
        foreach (var reader in _consumerReaders.Values)
        {
            var pendingOffset = reader.PendingOffset;

            if (minPendingOffset == null)
            {
                minPendingOffset = pendingOffset;
                continue;
            }

            if (pendingOffset < minPendingOffset)
            {
                minPendingOffset = pendingOffset;
            }
        }

        return minPendingOffset ?? _head.StartOffset;
    }

    // One reader can only be used by one consumer at the same time.
    private sealed class Reader
    {
        private MemoryBufferSegment<T> _currentSegment;
        private MemoryBufferPartitionOffset _pendingOffset;

        public Reader(MemoryBufferSegment<T> currentSegment, MemoryBufferPartitionOffset currentOffset)
        {
            _currentSegment = currentSegment;
            _pendingOffset = currentOffset;
        }

        public MemoryBufferPartitionOffset PendingOffset => _pendingOffset;

        public bool TryRead(out T item)
        {
            var segment = SelectSegment();
            return segment.TryGet(_pendingOffset, out item);
        }

        public void MoveNext() => _pendingOffset++;

        private MemoryBufferSegment<T> SelectSegment()
        {
            var currentSegment = _currentSegment;
            var nextSegment = currentSegment.NextSegment;
            var moveToNextSegment = currentSegment.EndOffset < _pendingOffset && nextSegment != null;

            if (moveToNextSegment)
            {
                _currentSegment = nextSegment!;
            }

            return _currentSegment;
        }
    }

    private class DebugView
    {
        private readonly MemoryBufferPartition<T> _partition;

        public DebugView(MemoryBufferPartition<T> partition)
        {
            _partition = partition;
        }

        public int PartitionId => _partition.PartitionId;

        public ulong Capacity => _partition.Capacity;

        public ulong Count => _partition.Count;

        public HashSet<MemoryBufferConsumer<T>> Consumers => _partition._consumers;

        public ConcurrentDictionary<string, Reader> ConsumerReaders => _partition._consumerReaders;

        public MemoryBufferSegment<T>[] Segments
        {
            get
            {
                var segments = new List<MemoryBufferSegment<T>>();
                for (var segment = _partition._head; segment != null; segment = segment.NextSegment)
                {
                    segments.Add(segment);
                }

                return segments.ToArray();
            }
        }
    }
}
