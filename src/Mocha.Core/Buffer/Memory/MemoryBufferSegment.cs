// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mocha.Core.Buffer.Memory;

[DebuggerDisplay("StartOffset = {StartOffset}, EndOffset = {EndOffset}")]
[DebuggerTypeProxy(typeof(MemoryBufferSegment<>.DebugView))]
internal sealed class MemoryBufferSegment<T>
{
    private readonly MemoryBufferPartitionOffset _startOffset;
    private readonly MemoryBufferPartitionOffset _endOffset;
    private readonly T[] _slots;
    private volatile int _reservedWritePosition;
    private volatile int _publishedWritePosition;

    public MemoryBufferSegment(int length, MemoryBufferPartitionOffset startOffset)
    {
        _startOffset = startOffset;
        _endOffset = startOffset + (ulong)(length - 1);
        _slots = new T[length];
        _reservedWritePosition = -1;
        _publishedWritePosition = -1;
    }

    private MemoryBufferSegment(T[] slots, MemoryBufferPartitionOffset startOffset)
    {
        _startOffset = startOffset;
        _endOffset = startOffset + (ulong)(slots.Length - 1);
        _slots = slots;
        _reservedWritePosition = -1;
        _publishedWritePosition = -1;
    }

    public MemoryBufferSegment<T>? NextSegment { get; set; }

    public MemoryBufferPartitionOffset StartOffset => _startOffset;

    public MemoryBufferPartitionOffset EndOffset => _endOffset;

    public int Capacity => _slots.Length;

    public int Count => Math.Min(Capacity, _publishedWritePosition + 1);

    public bool TryEnqueue(T item)
    {
        while (true)
        {
            var currentReserved = _reservedWritePosition;
            var nextPosition = currentReserved + 1;
            if (nextPosition >= _slots.Length)
            {
                // No more space to write in this segment.
                return false;
            }

            if (Interlocked.CompareExchange(
                    ref _reservedWritePosition,
                    nextPosition,
                    currentReserved)
                != currentReserved)
            {
                // Another thread has already written to the next position, retry.
                continue;
            }

            // Write the item to the slot.
            // It's safe to write directly without locks because each position is written by at most one thread.
            _slots[nextPosition] = item;

            // Now we need to publish the new write position so that readers can see the new item.
            while (true)
            {
                var currentPublished = _publishedWritePosition;
                if (currentPublished >= nextPosition)
                {
                    // Another thread has already published a position that is greater than our next position,
                    // which means our item is already visible to readers, no need to publish again.
                    break;
                }

                if (Interlocked.CompareExchange(
                        ref _publishedWritePosition,
                        nextPosition,
                        currentPublished)
                    == currentPublished)
                {
                    // Successfully published the new write position, now readers can see the new item.
                    break;
                }
            }

            return true;
        }
    }

    public bool TryGet(MemoryBufferPartitionOffset offset, int count, [NotNullWhen(true)] out T[]? items)
    {
        if (offset < _startOffset || offset > _endOffset)
        {
            items = null;
            return false;
        }

        var readPosition = (offset - _startOffset).ToInt32();

        if (_publishedWritePosition < 0 || readPosition > _publishedWritePosition)
        {
            items = null;
            return false;
        }

        var writePosition = Math.Min(_publishedWritePosition, _slots.Length - 1);
        // Number of items actually available to return (bounded by requested count and written items).
        var availableCount = Math.Min(count, writePosition - readPosition + 1);
        var wholeSegment = readPosition == 0 && availableCount == _slots.Length;
        if (wholeSegment)
        {
            items = _slots;
            return true;
        }

        items = _slots[readPosition..(readPosition + availableCount)];
        return true;
    }

    public MemoryBufferSegment<T> RecycleSlots(MemoryBufferPartitionOffset startOffset)
    {
        Array.Clear(_slots, 0, _slots.Length);
        return new MemoryBufferSegment<T>(_slots, startOffset);
    }

    private class DebugView
    {
        private readonly MemoryBufferSegment<T> _segment;

        public DebugView(MemoryBufferSegment<T> segment)
        {
            _segment = segment;
        }

        public MemoryBufferPartitionOffset StartOffset => _segment._startOffset;

        public MemoryBufferPartitionOffset EndOffset => _segment._endOffset;

        public int Capacity => _segment.Capacity;

        public int Count => _segment.Count;

        public T[] Items => _segment._slots.Take(_segment._publishedWritePosition + 1).ToArray();
    }
}
