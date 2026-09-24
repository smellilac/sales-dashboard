using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Data;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>
/// Spins up the API against a fresh, migrated database for one test (seed off), so tests are fully isolated
/// and run in parallel. Each call creates its own database in the shared container, applies migrations, seeds
/// hand-built data, then starts the app pointed at that database.
/// </summary>
internal static class DashboardTestHost
{
    /// <summary>Business time zone used by the API in tests and by the Moscow-wall-clock helpers (D4).</summary>
    public static readonly TimeZoneInfo Moscow = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");

    public static Task<DashboardApp> StartAsync(PostgresFixture postgres, Func<TestDataBuilder, Task> seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        return StartRawAsync(postgres, db => seed(new TestDataBuilder(db)));
    }

    /// <summary>
    /// Like <see cref="StartAsync"/> but hands the raw <see cref="SalesDbContext"/> to the seeder — for tests
    /// that insert generated seed data directly (CONSISTENCY-TEST) rather than the hand-built builder.
    /// </summary>
    public static async Task<DashboardApp> StartRawAsync(PostgresFixture postgres, Func<SalesDbContext, Task> seed)
    {
        ArgumentNullException.ThrowIfNull(postgres);
        ArgumentNullException.ThrowIfNull(seed);

        var connectionString = await postgres.CreateDatabaseAsync();

        await using (var db = PostgresFixture.CreateContext(connectionString))
        {
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
            await seed(db);
        }

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseSetting("ConnectionStrings:Sales", connectionString)
                .UseSetting("Database:ApplyMigrationsOnStartup", "false")
                .UseSetting("Reporting:TimeZone", "Europe/Moscow"));

        return new DashboardApp(factory);
    }

    /// <summary>A UTC instant for a Moscow wall-clock time — for placing sales at exact period boundaries (D4).</summary>
    public static DateTime MoscowUtc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0, int millisecond = 0)
    {
        var local = new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Moscow);
    }
}
