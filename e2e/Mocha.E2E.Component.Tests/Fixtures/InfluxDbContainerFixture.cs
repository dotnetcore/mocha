// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Testcontainers.InfluxDb;
using Xunit;

namespace Mocha.E2E.Component.Tests.Fixtures;

/// <summary>
/// Starts a real InfluxDB 2.7.7 container once for the whole test collection, configured to mirror
/// the compose stack (org/bucket/admin token). Testcontainers maps the container's 8086 to a random
/// host port, exposed via <see cref="Url"/>.
/// </summary>
public sealed class InfluxDbContainerFixture : IAsyncLifetime
{
    public const string Organization = "mocha_org";
    public const string Bucket = "mocha_metrics";
    public const string AdminToken = "mocha_influxdb_token";

    private readonly InfluxDbContainer _container = new InfluxDbBuilder()
        .WithImage("influxdb:2.7.7")
        .WithOrganization(Organization)
        .WithBucket(Bucket)
        .WithAdminToken(AdminToken)
        .WithUsername("mocha")
        .WithPassword("mocha_password")
        .Build();

    /// <summary>Base HTTP address of the running InfluxDB (random host port).</summary>
    public string Url { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Url = _container.GetAddress();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>xUnit collection so the InfluxDB container is created once and shared across tests.</summary>
[CollectionDefinition(Name)]
public sealed class InfluxDbCollection : ICollectionFixture<InfluxDbContainerFixture>
{
    public const string Name = "influxdb";
}
