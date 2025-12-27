// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.PromQL.Engine;

public interface IPromQLEngine
{
    Task<MatrixResult> QueryRangeAsync(string query,
        long startTimestampUnixSec,
        long endTimestampUnixSec,
        TimeSpan step,
        int? limit,
        CancellationToken cancellationToken);

    Task<IParseResult> QueryInstantAsync(string query,
        long timestampUnixSec,
        int? limit,
        CancellationToken cancellationToken);
}
