using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Tides.Api.Data;

[assembly: AssemblyFixture(typeof(Tides.Api.IntegrationTests.SqlServerFixture))]

namespace Tides.Api.IntegrationTests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public string ConnectionString => new SqlConnectionStringBuilder(container.GetConnectionString()) { InitialCatalog = "Tides" }.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<TidesDbContext>().UseSqlServer(ConnectionString).Options;
        await using var context = new TidesDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
