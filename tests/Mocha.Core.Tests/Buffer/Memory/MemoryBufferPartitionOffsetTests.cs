// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Tests.Buffer.Memory;

public class MemoryBufferPartitionOffsetTests
{
    [Fact]
    public void Plus()
    {
        var offset1 = new MemoryBufferPartitionOffset(1, 2);
        var offset2 = new MemoryBufferPartitionOffset(1, ulong.MaxValue);
        Assert.Equal(new MemoryBufferPartitionOffset(1, 5), offset1 + 3);
        Assert.Equal(new MemoryBufferPartitionOffset(2, 0), offset2 + 1);
    }

    [Fact]
    public void Minus()
    {
        var offset1 = new MemoryBufferPartitionOffset(0, 2);
        var offset2 = new MemoryBufferPartitionOffset(0, 1);
        var offset3 = new MemoryBufferPartitionOffset(1, 1);
        var offset4 = new MemoryBufferPartitionOffset(2, ulong.MaxValue);
        var offset5 = new MemoryBufferPartitionOffset(ulong.MaxValue, 3);
        var offset6 = new MemoryBufferPartitionOffset(ulong.MaxValue, ulong.MaxValue);

        Assert.Equal(new MemoryBufferPartitionOffset(0, 1), offset1 - 1);
        Assert.Equal(new MemoryBufferPartitionOffset(0, 1), offset1 - offset2);
        Assert.Equal(new MemoryBufferPartitionOffset(0, ulong.MaxValue), offset3 - offset1);
        Assert.Equal(new MemoryBufferPartitionOffset(0, ulong.MaxValue), offset3 - 2);
        Assert.Equal(new MemoryBufferPartitionOffset(1, ulong.MaxValue - 1), offset4 - offset3);
        Assert.Equal(new MemoryBufferPartitionOffset(ulong.MaxValue - 3, 4), offset5 - offset4);
        Assert.Equal(new MemoryBufferPartitionOffset(0, ulong.MaxValue - 3), offset6 - offset5);
        Assert.Equal(new MemoryBufferPartitionOffset(0, 3), offset1 - offset6);
        Assert.Throws<OverflowException>(() => offset1 - offset3);
        Assert.Throws<OverflowException>(() => offset5 - offset1);
    }

    [Fact]
    public void IncrementOperator()
    {
        var offset = new MemoryBufferPartitionOffset(0, 2);
        Assert.Equal(new MemoryBufferPartitionOffset(0, 3), ++offset);
        Assert.Equal(new MemoryBufferPartitionOffset(0, 3), offset++);
        Assert.Equal(new MemoryBufferPartitionOffset(0, 4), offset);
    }

    [Fact]
    public void DecrementOperator()
    {
        var offset = new MemoryBufferPartitionOffset(0, 2);
        Assert.Equal(new MemoryBufferPartitionOffset(0, 1), --offset);
        Assert.Equal(new MemoryBufferPartitionOffset(0, 1), offset--);
        Assert.Equal(new MemoryBufferPartitionOffset(0, 0), offset);
    }

    [Fact]
    public void Compare()
    {
        var offset1 = new MemoryBufferPartitionOffset(0, 1);
        var offset2 = new MemoryBufferPartitionOffset(0, 2);
        var offset3 = new MemoryBufferPartitionOffset(1, 0);
        var offset4 = new MemoryBufferPartitionOffset(ulong.MaxValue - 1, ulong.MaxValue);
        var offset5 = new MemoryBufferPartitionOffset(ulong.MaxValue, 1);
        var offset6 = new MemoryBufferPartitionOffset(ulong.MaxValue, ulong.MaxValue);
        var offset7 = new MemoryBufferPartitionOffset(ulong.MaxValue, ulong.MaxValue);

        Assert.True(offset2 > offset1);
        Assert.True(offset3 > offset2);
        Assert.True(offset4 > offset3);
        Assert.True(offset5 > offset4);
        Assert.True(offset6 > offset5);
        Assert.True(offset1 > offset6);
        Assert.True(offset1 > offset5);
        Assert.True(offset1 < offset4);
        Assert.True(offset7 == offset6);
    }

    [Fact]
    public void ToUInt64()
    {
        var offset1 = new MemoryBufferPartitionOffset(0, 1);
        var offset2 = new MemoryBufferPartitionOffset(0, ulong.MaxValue);
        var offset3 = new MemoryBufferPartitionOffset(1, 0);
        var offset4 = new MemoryBufferPartitionOffset(0, 2);

        Assert.Equal(1UL, offset1.ToUInt64());
        Assert.Equal(ulong.MaxValue, offset2.ToUInt64());
        Assert.Throws<OverflowException>(() => offset3.ToUInt64());
        Assert.Equal(2UL, (ulong)offset4);
    }

    [Fact]
    public void ToInt32()
    {
        var offset1 = new MemoryBufferPartitionOffset(0, 1);
        var offset2 = new MemoryBufferPartitionOffset(0, int.MaxValue);
        var offset3 = new MemoryBufferPartitionOffset(1, 0);
        var offset4 = new MemoryBufferPartitionOffset(0, 2);

        Assert.Equal(1, offset1.ToInt32());
        Assert.Equal(int.MaxValue, offset2.ToInt32());
        Assert.Throws<OverflowException>(() => offset3.ToInt32());
        Assert.Equal(2, (int)offset4);
    }
}
