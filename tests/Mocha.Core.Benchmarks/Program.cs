// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using BenchmarkDotNet.Running;
using Mocha.Core.Benchmarks;

var allBenchmarks = new[]
{
    typeof(MemoryBufferQueueProduceBenchmark),
    typeof(MemoryBufferQueueConsumeBenchmark),
};

new BenchmarkSwitcher(allBenchmarks).Run(args, new BenchmarkConfig());
