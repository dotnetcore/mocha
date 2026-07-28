// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Testcontainers.MySql;
using Xunit;

namespace Mocha.E2E.Component.Tests.Fixtures;

/// <summary>
/// Starts a real MySQL 8.2.0 container once for the whole test collection and seeds it with the
/// production schema from <c>scripts/mysql/init/*.sql</c> (copied next to the test assembly by the
/// csproj). Testcontainers maps the container's 3306 to a random host port, so this never collides
/// with a host-native MySQL on <c>:3306</c>.
/// </summary>
public sealed class MySqlContainerFixture : IAsyncLifetime
{
    // Mirrors the compose stack: same image tag, same database name.
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.2.0")
        .WithDatabase("mocha")
        .WithUsername("mocha")
        .WithPassword("mocha")
        .Build();

    /// <summary>ADO/EF connection string for the running container (random host port).</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        await SeedSchemaAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private async Task SeedSchemaAsync()
    {
        var initDir = Path.Combine(AppContext.BaseDirectory, "mysql-init");
        var scripts = Directory.GetFiles(initDir, "*.sql").OrderBy(p => p, StringComparer.Ordinal);

        foreach (var script in scripts)
        {
            var sql = await File.ReadAllTextAsync(script);
            // Testcontainers' ExecScriptAsync pipes the file through the mysql client, which
            // understands multi-statement scripts including CREATE DATABASE/USE.
            var result = await _container.ExecScriptAsync(sql);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Seeding '{Path.GetFileName(script)}' failed (exit {result.ExitCode}): {result.Stderr}");
            }
        }
    }
}

/// <summary>xUnit collection so the MySQL container is created once and shared across tests.</summary>
[CollectionDefinition(Name)]
public sealed class MySqlCollection : ICollectionFixture<MySqlContainerFixture>
{
    public const string Name = "mysql";
}
