using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace SalesDashboard.Api.Data;

/// <summary>
/// Applies pending EF Core migrations at startup when <see cref="DatabaseOptions.ApplyMigrationsOnStartup"/>
/// is enabled (DB-INIT). This is the only place the API performs DDL. Seed is out of scope for T2.
/// </summary>
public sealed class SalesDbInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<DatabaseOptions> options,
    ILogger<SalesDbInitializer> logger) : IHostedService
{
    private readonly DatabaseOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.ApplyMigrationsOnStartup)
        {
            logger.LogInformation(
                "Skipping database migration on startup: {Option} is disabled",
                nameof(DatabaseOptions.ApplyMigrationsOnStartup));
            return;
        }

        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

            var pending = (await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken)
                .ConfigureAwait(false)).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("Database schema is up to date: no pending migrations");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            logger.LogInformation(
                "Applied {MigrationCount} database migration(s) in {ElapsedMs} ms: {Migrations}",
                pending.Count,
                stopwatch.ElapsedMilliseconds,
                pending);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
