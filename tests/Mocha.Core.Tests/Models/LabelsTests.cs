// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Metrics;

namespace Mocha.Core.Tests.Models;

public class LabelsTests
{
    [Fact]
    public void Labels_Equals_SameLabels_ReturnsTrue()
    {
        var labels1 = new Labels { { "label1", "value1" }, { "label2", "value2" } };

        var labels2 = new Labels { { "label1", "value1" }, { "label2", "value2" } };

        Assert.True(labels1.Equals(labels2));
    }

    [Fact]
    public void Labels_Equals_DifferentLabels_ReturnsFalse()
    {
        var labels1 = new Labels { { "label1", "value1" }, { "label2", "value2" } };
        var labels2 = new Labels { { "label1", "value1" }, { "label2", "differentValue" } };
        Assert.False(labels1.Equals(labels2));
    }

    [Fact]
    public void Labels_GetHashCode_SameLabels_SameHashCode()
    {
        var labels1 = new Labels { { "label1", "value1" }, { "label2", "value2" } };
        var labels2 = new Labels { { "label1", "value1" }, { "label2", "value2" } };
        Assert.Equal(labels1.GetHashCode(), labels2.GetHashCode());
    }


    [Fact]
    public void Labels_GetHashCode_DifferentLabels_DifferentHashCode()
    {
        var labels1 = new Labels { { "label1", "value1" }, { "label2", "value2" } };
        var labels2 = new Labels { { "label1", "value1" }, { "label2", "differentValue" } };
        Assert.NotEqual(labels1.GetHashCode(), labels2.GetHashCode());
    }

    [Fact]
    public void Labels_HashSet_Contains_CorrectlyIdentifiesEqualLabels()
    {
        var labels1 = new Labels { { "label1", "value1" }, { "label2", "value2" } };
        var labels2 = new Labels { { "label1", "value1" }, { "label2", "value2" } };

        var hashSet = new HashSet<Labels> { labels1 };

        Assert.Contains(labels2, hashSet);
    }

    [Fact]
    public void Labels_HashSet_DoesNotContain_DifferentLabels()
    {
        var labels1 = new Labels { { "label1", "value1" }, { "label2", "value2" } };
        var labels2 = new Labels { { "label1", "value1" }, { "label2", "differentValue" } };

        var hashSet = new HashSet<Labels> { labels1 };

        Assert.DoesNotContain(labels2, hashSet);
    }

    [Fact]
    public void Labels_DictionaryKey_WorksAsExpected()
    {
        var labels1 = new Labels { { "label1", "value1" }, { "label2", "value2" } };
        var labels2 = new Labels { { "label1", "value1" }, { "label2", "value2" } };

        var dictionary = new Dictionary<Labels, string>
        {
            { labels1, "TestValue" }
        };

        Assert.True(dictionary.TryGetValue(labels2, out var value));
        Assert.Equal("TestValue", value);
    }
}
