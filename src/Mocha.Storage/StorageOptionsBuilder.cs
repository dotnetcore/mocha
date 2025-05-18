// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Storage;

public class StorageOptionsBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;

    public StorageOptionsBuilder WithMetadata(Action<MetadataStorageOptionsBuilder> configure)
    {
        configure(new MetadataStorageOptionsBuilder(Services));
        return this;
    }

    public StorageOptionsBuilder WithTracing(Action<TracingStorageOptionsBuilder> configure)
    {
        configure(new TracingStorageOptionsBuilder(Services));
        return this;
    }

    public StorageOptionsBuilder WithMetrics(Action<MetricsStorageOptionsBuilder> configure)
    {
        configure(new MetricsStorageOptionsBuilder(Services));
        return this;
    }
}
