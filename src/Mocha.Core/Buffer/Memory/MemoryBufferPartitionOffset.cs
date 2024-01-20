using System.Diagnostics;

namespace Mocha.Core.Buffer.Memory;

[DebuggerDisplay("Generation = {Generation}, Index = {Index}")]
internal readonly record struct MemoryBufferPartitionOffset(ulong Generation, ulong Index)
{
    public ulong ToUInt64()
    {
        if (Generation == 0)
        {
            return Index;
        }

        throw new OverflowException("Offset is too large to be converted to UInt64.");
    }

    public int ToInt32()
    {
        if (Generation == 0 && Index <= int.MaxValue)
        {
            return (int)Index;
        }

        throw new OverflowException("Offset is too large to be converted to Int32.");
    }

    public static explicit operator ulong(MemoryBufferPartitionOffset offset) => offset.ToUInt64();

    public static explicit operator int(MemoryBufferPartitionOffset offset) => offset.ToInt32();

    public static bool operator >(MemoryBufferPartitionOffset left, MemoryBufferPartitionOffset right)
    {
        var leftGeneration = left.Generation;
        var rightGeneration = right.Generation;

        // Consider that leftGeneration has overflowed to 0, it is greater than rightGeneration.
        if (leftGeneration == 0 && rightGeneration == ulong.MaxValue)
        {
            return true;
        }

        if (leftGeneration == ulong.MaxValue && rightGeneration == 0)
        {
            return false;
        }

        return leftGeneration > rightGeneration ||
               leftGeneration == rightGeneration && left.Index > right.Index;
    }

    public static bool operator <(MemoryBufferPartitionOffset left, MemoryBufferPartitionOffset right) =>
        left != right && left > right == false;

    public static MemoryBufferPartitionOffset operator -(
        MemoryBufferPartitionOffset left,
        MemoryBufferPartitionOffset right)
    {
        if (left.Generation == 0 && right.Generation == ulong.MaxValue)
        {
            return left with { Generation = 1 } - right with { Generation = 0 };
        }

        if (left < right)
        {
            throw new OverflowException("Cannot subtract a larger offset from a smaller offset.");
        }

        if (left.Generation == right.Generation)
        {
            return new MemoryBufferPartitionOffset(0, left.Index - right.Index);
        }

        if (left.Index >= right.Index)
        {
            return new MemoryBufferPartitionOffset(left.Generation - right.Generation, left.Index - right.Index);
        }

        return new MemoryBufferPartitionOffset(
            left.Generation - right.Generation - 1,
            ulong.MaxValue - right.Index + left.Index + 1);
    }

    public static MemoryBufferPartitionOffset operator +(MemoryBufferPartitionOffset offset, ulong value)
    {
        var generation = offset.Generation;
        var index = offset.Index + value;
        if (index < offset.Index)
        {
            generation++;
        }

        return new MemoryBufferPartitionOffset(generation, index);
    }

    public static MemoryBufferPartitionOffset operator -(MemoryBufferPartitionOffset offset, ulong value)
    {
        var generation = offset.Generation;
        var index = offset.Index - value;
        if (index > offset.Index)
        {
            generation--;
        }

        return new MemoryBufferPartitionOffset(generation, index);
    }

    public static MemoryBufferPartitionOffset operator ++(MemoryBufferPartitionOffset offset) => offset + 1;

    public static MemoryBufferPartitionOffset operator --(MemoryBufferPartitionOffset offset) => offset - 1;
}
