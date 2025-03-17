// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Mocha.Core.Extensions;

namespace Mocha.Core.Storage.Prometheus;

public partial class Labels : Dictionary<string, string>, IEquatable<Labels>
{
    public const string MetricName = "__name__";

    public const string BucketLabel = "le";

    public static readonly Labels Empty = [];

    private static readonly Regex _labelNameRegex = GenerateLabelNameRegex();

    public Labels()
    {
    }

    public Labels(IDictionary<string, string> dictionary) : base(dictionary)
    {
    }

    public Labels DropMetricName() => Drop(MetricName);

    public Labels Drop(params string[] keys)
    {
        var newLabels = new Labels(this);

        foreach (var key in keys)
        {
            newLabels.Remove(key);
        }

        return newLabels;
    }

    /// <summary>
    /// Returns a subset of Labels that matches/does not match with the provided <see cref="names"/> based on the 'on' boolean.
    /// </summary>
    /// <param name="on">If on is set to true, it returns the subset of labels that match with the provided <see cref="names"/> and its inverse when 'on' is set to false.</param>
    /// <param name="names">The names of the labels to include or exclude from the Labels.</param>
    /// <returns>A subset of Labels.</returns>
    public Labels MatchLabels(bool on, HashSet<string> names)
    {
        var newLabels = new Labels();

        foreach (var (key, value) in this)
        {
            if (key == MetricName)
            {
                continue;
            }

            var shouldAdd = on ? names.Contains(key) : !names.Contains(key);

            if (shouldAdd)
            {
                newLabels.Add(key, value);
            }
        }

        return newLabels;
    }

    public bool Equals(Labels? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Count != other.Count)
        {
            return false;
        }

        foreach (var kvp in this)
        {
            if (!other.TryGetValue(kvp.Key, out var otherValue))
            {
                return false;
            }

            if (kvp.Value != otherValue)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Labels)obj);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var kvp in this.OrderBy(kvp => kvp.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }

        return hash.ToHashCode();
    }

    public override string ToString() => this.ToJson();

    public static bool IsLabelNameValid(string labelName) =>
        string.IsNullOrWhiteSpace(labelName) == false &&
        _labelNameRegex.IsMatch(labelName);

    public static LabelsBuilder Builder(Labels labels) => new(labels);

    public static LabelsBuilder Builder() => new();

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex GenerateLabelNameRegex();
}
