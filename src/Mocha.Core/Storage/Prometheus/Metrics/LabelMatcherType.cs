// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Prometheus.Metrics;

public enum LabelMatcherType
{
    Equal,
    NotEqual,
    RegexMatch,
    RegexNotMatch
}
