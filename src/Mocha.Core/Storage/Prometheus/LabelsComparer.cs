// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Prometheus;

public class LabelsComparer : IEqualityComparer<Labels>
{
    public bool Equals(Labels? x, Labels? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        if (x.Count != y.Count)
        {
            return false;
        }

        foreach (var (label, value) in x)
        {
            if (!y.TryGetValue(label, out var otherValue))
            {
                return false;
            }

            if (value != otherValue)
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(Labels labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        var hash = new HashCode();

        foreach (var (label, value) in labels.OrderBy(kvp => kvp.Key))
        {
            hash.Add(label);
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}
