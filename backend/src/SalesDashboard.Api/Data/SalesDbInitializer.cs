using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data.Seed;

namespace SalesDashboard.Api.Data;

/// <summary>
/// Applies pending migrations and then seeds the database at startup (DB-INIT). This is the only place the
/// API runs DDL; it is gated by <c>Database:ApplyMigrationsOnStartup</c>, which is on only in docker-compose.
/// </summary>
public sealed class SalesDbInitializer(
    IServiceProvider services,
    IOptions<DatabaseOptions> databaseOptions,
    ILogger<SalesDbInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!databaseOptions.Value.ApplyMigrationsOnStartup)
        {
            return;
        }

        var scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

            var seeder = scope.ServiceProvider.GetRequiredService<SalesDataSeeder>();
            var stopwatch = Stopwatch.StartNew();
            var result = await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (result is null)
            {
                logger.LogInformation("Seed skipped: sales data already present");
            }
            else
            {
                logger.LogInformation(
                    "Seed completed in {ElapsedMs} ms (as of {GeneratedAt:o}): {Managers} managers, {Customers} customers, {Categories} categories, {Products} products, {Sales} sales, {SaleItems} sale items",
                    stopwatch.ElapsedMilliseconds,
                    result.GeneratedAt,
                    result.Managers,
                    result.Customers,
                    result.Categories,
                    result.Products,
                    result.Sales,
                    result.SaleItems);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
