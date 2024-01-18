// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Extensions;

namespace Mocha.Core.Tests.Extensions;

public class JsonExtensions
{
    [Fact]
    public void ToJson_ReturnsCorrectValue()
    {
        var expected = "{\"id\":1,\"name\":\"Test\"}";
        var obj = new Foo { Id = 1, Name = "Test" };

        var actual = obj.ToJson();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromJson_ReturnsCorrectValue()
    {
        var json = "{\"id\":1,\"name\":\"Test\"}";
        var expected = new Foo { Id = 1, Name = "Test" };

        var actual = json.FromJson<Foo>();

        Assert.Equivalent(expected, actual);
    }

    private class Foo
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
