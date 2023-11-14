// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Mocha.Core.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        Add(DefaultConfig.Instance);
        AddDiagnoser(MemoryDiagnoser.Default);

        ArtifactsPath = Path.Combine(AppContext.BaseDirectory, "artifacts",
            DateTime.Now.ToString("yyyy-mm-dd_hh-MM-ss"));
    }
}
