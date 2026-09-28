using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Shared.Managers;

/// <summary>
/// Reads each manager's Paid aggregates for the current and previous period in a single EF Core query
/// (D2, D10). The two periods are contiguous (<c>Previous.EndUtc == Current.StartUtc</c>), so both are read
/// from one span split at <see cref="ReportingPeriod.Current"/>'s start — no double scan. Money is summed in
/// SQL from the generated line columns (D5); line rows never reach memory. Every manager is returned,
/// including those with no Paid sales (LEFT JOIN); the visibility rule (D7) is applied later by
/// <see cref="CompetitionRanking"/>, not here.
/// </summary>
public static class ManagerPerformanceReader
{
    public static async Task<IReadOnlyList<ManagerPerformanceRow>> ReadAsync(
        SalesDbContext db,
        ReportingPeriod period,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(period);

        var spanStart = period.Previous.StartUtc;
        var spanEnd = period.Current.EndUtc;
        var split = period.Current.StartUtc;

        // Per-sale money from the generated columns (one row per sale); SUM is coalesced so a SQL NULL (empty
        // set once EF flattens the join) never lands in a non-nullable decimal.
        var saleMoney =
            from i in db.SaleItems.AsNoTracking()
            group i by i.SaleId into g
            select new { SaleId = g.Key, Revenue = g.Sum(x => (decimal?)x.LineRevenue) ?? 0m, Cost = g.Sum(x => (decimal?)x.LineCost) ?? 0m };

        // Paid sales in the combined span, tagged with the period they fall in.
        // Inner join is safe: every sale has at least one line (D13).
        var paidSales =
            from s in db.Sales.AsNoTracking()
            where s.Status == SaleStatus.Paid && s.SoldAt >= spanStart && s.SoldAt < spanEnd
            join money in saleMoney on s.Id equals money.SaleId
            select new { s.ManagerId, IsCurrent = s.SoldAt >= split, money.Revenue, money.Cost };

        var aggregates =
            from x in paidSales
            group x by x.ManagerId into g
            select new
            {
                ManagerId = g.Key,
                // Nullable on purpose: the LEFT JOIN below has no match for managers with no sales, so every
                // aggregate arrives as SQL NULL and is coalesced there. Filtered sums cover the other-period side.
                CurrentRevenue = g.Where(x => x.IsCurrent).Sum(x => (decimal?)x.Revenue),
                CurrentCost = g.Where(x => x.IsCurrent).Sum(x => (decimal?)x.Cost),
                CurrentSalesCount = (int?)g.Count(x => x.IsCurrent),
                PreviousRevenue = g.Where(x => !x.IsCurrent).Sum(x => (decimal?)x.Revenue),
                PreviousCost = g.Where(x => !x.IsCurrent).Sum(x => (decimal?)x.Cost),
                PreviousSalesCount = (int?)g.Count(x => !x.IsCurrent),
            };

        var query =
            from m in db.Managers.AsNoTracking()
            join a in aggregates on m.Id equals a.ManagerId into ga
            from a in ga.DefaultIfEmpty()
            select new ManagerPerformanceRow
            {
                ManagerId = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Team = m.Team,
                IsActive = m.IsActive,
                CurrentRevenue = a.CurrentRevenue ?? 0m,
                CurrentCost = a.CurrentCost ?? 0m,
                CurrentSalesCount = a.CurrentSalesCount ?? 0,
                PreviousRevenue = a.PreviousRevenue ?? 0m,
                PreviousCost = a.PreviousCost ?? 0m,
                PreviousSalesCount = a.PreviousSalesCount ?? 0,
            };

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
