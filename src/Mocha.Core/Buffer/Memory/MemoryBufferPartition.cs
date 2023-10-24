// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mocha.Core.Buffer.Memory;

internal class MemoryBufferPartition<T>
{
    // internal for testing
    internal static int SegmentLength = 1024;

    private volatile MemoryBufferSegment<T> _head;
    private volatile MemoryBufferSegment<T> _tail;

    // At most one consumer per group,
    // different consumers in a group will not access concurrently at the same time.
    private readonly ConcurrentDictionary<string /* group name */, Reader> _consumerReaders;

    private readonly object _createSegmentLock;

    public MemoryBufferPartition()
    {
        _head = _tail = new MemoryBufferSegment<T>(SegmentLength, default);
        _consumerReaders = new ConcurrentDictionary<string, Reader>();
        _createSegmentLock = new object();
    }

    public void Enqueue(T item)
    {
        while (true)
        {
            var tail = _tail;

            if (tail.TryEnqueue(item))
            {
                foreach (var reader in _consumerReaders.Values)
                {
                    reader.OnWrite(item);
                }

                return;
            }

            lock (_createSegmentLock)
            {
                var newSegment = TryRecycleSegment(out var recycledSegment)
                    ? recycledSegment
                    : new MemoryBufferSegment<T>(SegmentLength, tail.EndOffset + 1);
                tail.NextSegment = newSegment;
                _tail = newSegment;
            }
        }
    }

    public ValueTask<T> PullAsync(string groupName)
    {
        var reader = _consumerReaders.AddOrUpdate(
            groupName,
            _ => new Reader(_head, _head.StartOffset),
            (_, reader) => reader);

        return reader.ReadAsync();
    }

    public void Commit(string groupName)
    {
        if (!_consumerReaders.TryGetValue(groupName, out var reader))
        {
            throw new InvalidOperationException("Specified group name not found.");
        }

        reader.MoveNext();
    }


    private bool TryRecycleSegment([NotNullWhen(true)] out MemoryBufferSegment<T>? recycledSegment)
    {
        recycledSegment = null;

        if (_head == _tail)
        {
            return false;
        }

        var minConsumerOffset = MinConsumerOffset();

        for (var segment = _head; segment != _tail; segment = segment.NextSegment!)
        {
            if (segment.EndOffset < minConsumerOffset)
            {
                recycledSegment = segment;
            }
        }

        if (recycledSegment == null)
        {
            return false;
        }

        _head = recycledSegment.NextSegment!;
        recycledSegment.NextSegment = null;

        return true;
    }

    private Offset MinConsumerOffset()
    {
        Offset? minConsumerOffset = null;
        foreach (var reader in _consumerReaders.Values)
        {
            if (minConsumerOffset == null)
            {
                minConsumerOffset = reader.CurrentOffset;
                continue;
            }

            var offset = reader.CurrentOffset;
            if (offset < minConsumerOffset)
            {
                minConsumerOffset = offset;
            }
        }

        return minConsumerOffset ?? _head.StartOffset;
    }


    // offset may exceed ulong.MaxValue, creat new generation to avoid this
    [DebuggerDisplay("Generation = {_generation}, Index = {_index}")]
    internal struct Offset
    {
        // TODO: handle generation overflow
        private ulong _generation;
        private ulong _index;

        public override bool Equals(object? obj)
        {
            return obj is Offset offset && this == offset;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_generation, _index);
        }

        public ulong ToUInt64()
        {
            if (_generation == 0)
            {
                return _index;
            }

            throw new InvalidOperationException("Offset is too large to be converted to UInt64.");
        }

        public static bool operator >(Offset left, Offset right)
        {
            return left._generation > right._generation ||
                   left._generation == right._generation && left._index > right._index;
        }

        public static bool operator <(Offset left, Offset right)
        {
            return left._generation < right._generation ||
                   left._generation == right._generation && left._index < right._index;
        }

        public static bool operator ==(Offset left, Offset right)
        {
            return left._generation == right._generation && left._index == right._index;
        }

        public static bool operator !=(Offset left, Offset right)
        {
            return left._generation != right._generation || left._index != right._index;
        }

        public static Offset operator -(Offset offset, Offset value)
        {
            if (offset < value)
            {
                throw new InvalidOperationException("Cannot subtract a larger offset from a smaller offset.");
            }

            if (offset._generation == value._generation)
            {
                return new Offset { _generation = 0, _index = offset._index - value._index };
            }

            if (offset._index >= value._index)
            {
                return new Offset
                {
                    _generation = offset._generation - value._generation, _index = offset._index - value._index
                };
            }

            return new Offset
            {
                _generation = offset._generation - value._generation - 1,
                _index = ulong.MaxValue - value._index + offset._index + 1
            };
        }

        public static Offset operator ++(Offset offset)
        {
            var generation = offset._generation;
            if (offset._index == ulong.MaxValue)
            {
                generation++;
            }

            return new Offset { _generation = generation, _index = offset._index + 1 };
        }

        public static Offset operator +(Offset offset, ulong value)
        {
            var generation = offset._generation;
            if (offset._index == ulong.MaxValue)
            {
                generation++;
            }

            return new Offset { _generation = generation, _index = offset._index + value };
        }
    }

    private sealed class Reader
    {
        private MemoryBufferSegment<T> _currentSegment;
        private Offset _currentOffset;

        private volatile TaskCompletionSource<T>? _tcs;
        private readonly ReaderWriterLockSlim _tcsLock;

        public Reader(MemoryBufferSegment<T> currentSegment, Offset currentOffset)
        {
            _currentSegment = currentSegment;
            _currentOffset = currentOffset;
            _tcsLock = new ReaderWriterLockSlim();
        }

        public Offset CurrentOffset => _currentOffset;

        public ValueTask<T> ReadAsync()
        {
            if (_tcs != null)
            {
                throw new InvalidOperationException("Cannot read concurrently.");
            }

            if (_currentSegment.TryGet(_currentOffset, out var item))
            {
                return new ValueTask<T>(item);
            }

            var nextSegment = _currentSegment.NextSegment;
            var moveToNextSegment = _currentSegment.EndOffset < _currentOffset && nextSegment != null;

            if (moveToNextSegment)
            {
                _currentSegment = nextSegment!;
                _currentOffset = nextSegment!.StartOffset;
                return ReadAsync();
            }

            _tcsLock.EnterWriteLock();
            _tcs = new TaskCompletionSource<T>();
            _tcsLock.ExitWriteLock();
            return new ValueTask<T>(_tcs.Task);
        }

        public void MoveNext()
        {
            _currentOffset++;
        }

        public void OnWrite(T item)
        {
            _tcsLock.EnterReadLock();
            var tcs = _tcs;
            if (tcs == null)
            {
                return;
            }

            _tcsLock.ExitReadLock();

            _tcs = null;
            tcs.SetResult(item);
        }
    }
}
