// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Prometheus;

public sealed class LabelsBuilder
{
    private readonly Labels _labels;

    public LabelsBuilder(Labels labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        _labels = new Labels(labels);
    }

    public LabelsBuilder() : this([])
    {
    }

    public LabelsBuilder Add(string label, string value)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(value);

        _labels[label] = value;

        return this;
    }

    public LabelsBuilder AddRange(IEnumerable<KeyValuePair<string, string>> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        foreach (var (label, value) in labels)
        {
            _labels[label] = value;
        }

        return this;
    }

    public LabelsBuilder Remove(string label)
    {
        ArgumentNullException.ThrowIfNull(label);

        _labels.Remove(label);

        return this;
    }

    public LabelsBuilder RemoveRange(IEnumerable<string>? labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        foreach (var label in labels)
        {
            _labels.Remove(label);
        }

        return this;
    }

    public Labels Build() => _labels;
}
