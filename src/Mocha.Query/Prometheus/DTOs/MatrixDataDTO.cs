// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.DTOs;

public class MatrixDataDTO
{
    public Dictionary<string, string>? Metric { get; set; }

    public List<object[]>? Values { get; set; }
}
