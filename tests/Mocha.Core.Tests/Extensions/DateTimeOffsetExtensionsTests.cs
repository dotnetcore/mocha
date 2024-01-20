// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Extensions;

namespace Mocha.Core.Tests.Extensions;

public class DateTimeOffsetExtensionsTests
{
    [Fact]
    public void ToUnixTimeNanoseconds_ReturnsCorrectValue()
    {
        var dateTimeOffset = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expected = 1609459200000000000UL;

        var actual = dateTimeOffset.ToUnixTimeNanoseconds();

        Assert.Equal(expected, actual);
    }
}
