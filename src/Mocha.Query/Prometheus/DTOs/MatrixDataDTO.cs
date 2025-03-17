// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus;

namespace Mocha.Query.Prometheus.DTOs;

public class MatrixDataDTO
{
    public required Labels Metric { get; set; }

    public required List<object[]> Values { get; set; }
}
