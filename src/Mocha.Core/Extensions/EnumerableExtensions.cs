// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Extensions;

public static class EnumerableExtensions
{
    public static double StandardVariance(this IEnumerable<double> values)
    {
        var count = 0;
        var sum = 0.0;
        var sumSq = 0.0;
        foreach (var value in values)
        {
            count++;
            sum += value;
            sumSq += value * value;
        }

        if (count == 0)
        {
            return 0;
        }

        var mean = sum / count;
        return Math.Max(0, sumSq / count - mean * mean);
    }

    public static double StandardDeviation(this IEnumerable<double> values) => Math.Sqrt(values.StandardVariance());
}
