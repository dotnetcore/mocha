// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Contexts;

public class QueryInstantVectorContext : IPromQLParametersContext
{
    public required long TimestampUnixSec { get; set; }
}
