using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Shared;

namespace SalesDashboard.Api.Data.Seed;

/// <summary>
/// Writes generated seed data in a single transaction. Idempotent: seeds only when the database is empty
/// (D13, DB-INIT). "Now" comes from <see cref="TimeProvider"/> and the business zone from
/// <see cref="ReportingOptions"/>, so the generated data (and its ids) is reproducible for a given run day.
/// </summary>
public sealed class SalesDataSeeder(
    SalesDbContext db,
    TimeProvider timeProvider,
    IOptions<ReportingOptions> reportingOptions)
{
    /// <summary>Seeds an empty database; returns the counts, or <see langword="null"/> if data was already present.</summary>
    public async Task<SeedResult?> SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Managers.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(reportingOptions.Value.TimeZone);
        var now = timeProvider.GetUtcNow();
        var data = SeedDataGenerator.Generate(now, zone);

        // Bulk insert: skip per-entity change detection (the graph is built once, never mutated).
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        db.Managers.AddRange(data.Managers);
        db.Customers.AddRange(data.Customers);
        db.Categories.AddRange(data.Categories);
        db.Products.AddRange(data.Products);
        db.Sales.AddRange(data.Sales);
        db.SaleItems.AddRange(data.SaleItems);

        // SaveChangesAsync wraps the inserts in one transaction.
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SeedResult(
            now,
            data.Managers.Count,
            data.Customers.Count,
            data.Categories.Count,
            data.Products.Count,
            data.Sales.Count,
            data.SaleItems.Count);
    }
}
