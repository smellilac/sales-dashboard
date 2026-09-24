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

        // Per-sale money, summed in SQL from the generated columns (one row per sale).
        var saleMoney =
            from i in db.SaleItems.AsNoTracking()
            group i by i.SaleId into g
            select new { SaleId = g.Key, Revenue = g.Sum(x => x.LineRevenue), Cost = g.Sum(x => x.LineCost) };

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
                CurrentRevenue = g.Sum(x => x.IsCurrent ? x.Revenue : 0m),
                CurrentCost = g.Sum(x => x.IsCurrent ? x.Cost : 0m),
                CurrentSalesCount = g.Sum(x => x.IsCurrent ? 1 : 0),
                PreviousRevenue = g.Sum(x => x.IsCurrent ? 0m : x.Revenue),
                PreviousCost = g.Sum(x => x.IsCurrent ? 0m : x.Cost),
                PreviousSalesCount = g.Sum(x => x.IsCurrent ? 0 : 1),
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
                CurrentRevenue = a == null ? 0m : a.CurrentRevenue,
                CurrentCost = a == null ? 0m : a.CurrentCost,
                CurrentSalesCount = a == null ? 0 : a.CurrentSalesCount,
                PreviousRevenue = a == null ? 0m : a.PreviousRevenue,
                PreviousCost = a == null ? 0m : a.PreviousCost,
                PreviousSalesCount = a == null ? 0 : a.PreviousSalesCount,
            };

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
