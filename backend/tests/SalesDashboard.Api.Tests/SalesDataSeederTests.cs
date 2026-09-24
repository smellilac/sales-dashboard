using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Data.Seed;
using SalesDashboard.Api.Shared;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>Seeder tests against a real PostgreSQL container: fresh insert and idempotent re-run (D13).</summary>
public sealed class SalesDataSeederTests(PostgresFixture postgres)
{
    private const string Zone = "Europe/Moscow";
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.FromHours(3));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SalesDataSeeder CreateSeeder(SalesDbContext db) => new(
        db,
        new FakeTimeProvider(Now),
        Options.Create(new ReportingOptions { TimeZone = Zone }));

    [Fact]
    public async Task Seed_OnEmptyDatabase_InsertsGeneratedData()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var db = PostgresFixture.CreateContext(connectionString);
        await db.Database.MigrateAsync(Ct);

        var result = await CreateSeeder(db).SeedAsync(Ct);

        Assert.NotNull(result);

        var expected = SeedDataGenerator.Generate(Now, TimeZoneInfo.FindSystemTimeZoneById(Zone));

        await using var readDb = PostgresFixture.CreateContext(connectionString);
        Assert.Equal(expected.Managers.Count, await readDb.Managers.CountAsync(Ct));
        Assert.Equal(expected.Customers.Count, await readDb.Customers.CountAsync(Ct));
        Assert.Equal(expected.Categories.Count, await readDb.Categories.CountAsync(Ct));
        Assert.Equal(expected.Products.Count, await readDb.Products.CountAsync(Ct));
        Assert.Equal(expected.Sales.Count, await readDb.Sales.CountAsync(Ct));
        Assert.Equal(expected.SaleItems.Count, await readDb.SaleItems.CountAsync(Ct));
        Assert.Equal(expected.Sales.Count, result.Sales);

        // Generated columns are computed by the database on insert (D5).
        var item = await readDb.SaleItems.FirstAsync(Ct);
        Assert.Equal(item.Quantity * item.UnitPrice, item.LineRevenue);
        Assert.Equal(item.Quantity * item.UnitCost, item.LineCost);
    }

    [Fact]
    public async Task Seed_WhenDataAlreadyPresent_IsNoOp()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var db = PostgresFixture.CreateContext(connectionString);
        await db.Database.MigrateAsync(Ct);

        var first = await CreateSeeder(db).SeedAsync(Ct);
        Assert.NotNull(first);

        await using var secondContext = PostgresFixture.CreateContext(connectionString);
        var second = await CreateSeeder(secondContext).SeedAsync(Ct);
        Assert.Null(second);

        await using var readDb = PostgresFixture.CreateContext(connectionString);
        Assert.Equal(first.Sales, await readDb.Sales.CountAsync(Ct));
        Assert.Equal(first.SaleItems, await readDb.SaleItems.CountAsync(Ct));
    }
}
