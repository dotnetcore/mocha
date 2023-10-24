// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Reflection;
using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Tests.Buffer.Memory;

public class MemoryBufferPartitionTests
{
    [Fact]
    public async Task Segment_Will_Be_Recycled_If_All_Consumers_Consumed()
    {
        MemoryBufferPartition<int>.SegmentLength = 3;

        var partition = new MemoryBufferPartition<int>();

        for (var i = 0; i < 6; i++)
        {
            partition.Enqueue(i);
        }

        var segments1 = GetSegments(partition);

        for (var i = 0; i < 3; i++)
        {
            await partition.PullAsync("TestGroup");
            partition.Commit("TestGroup");
        }
        partition.Enqueue(7);

        var segments2 = GetSegments(partition);

        Assert.Equal(segments1[0], segments2[1]);
        Assert.Equal(segments1[1], segments2[0]);
    }

    private List<MemoryBufferSegment<int>> GetSegments(MemoryBufferPartition<int> partition)
    {
        var head = typeof(MemoryBufferPartition<>)
            .MakeGenericType(typeof(int))
            .GetField("_head", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(partition) as MemoryBufferSegment<int>;

        var segments = new List<MemoryBufferSegment<int>>();
        var segment = head;
        while (segment != null)
        {
            segments.Add(segment);
            segment = segment.NextSegment;
        }

        return segments;
    }
}
