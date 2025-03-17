// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Engine;

public class PromQLEngineOptions
{
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan LookBackDelta { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan SubqueryStepInterval { get; set; } = TimeSpan.FromMinutes(1);

    public int MaxSamplesPerQuery { get; set; } = 50000000;

    public TimeSpan DefaultEvaluationInterval { get; set; } = TimeSpan.FromMinutes(15);
}
