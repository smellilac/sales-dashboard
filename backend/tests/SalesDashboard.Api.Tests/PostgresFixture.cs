using Microsoft.EntityFrameworkCore;
using Npgsql;
using SalesDashboard.Api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>
/// One PostgreSQL 17 container shared by the whole test assembly (registered via
/// <c>[assembly: AssemblyFixture(typeof(PostgresFixture))]</c>). Each test asks for a fresh, empty
/// database so tests do not interfere with one another.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .Build();

    /// <summary>Connection string to the container's default database.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>Creates a new empty database in the container and returns its connection string.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var databaseName = "test_" + Guid.NewGuid().ToString("N");

        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName,
        }.ConnectionString;
    }

    /// <summary>Builds a context wired exactly like the app (snake_case naming), for the given database.</summary>
    public static SalesDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options);
}
