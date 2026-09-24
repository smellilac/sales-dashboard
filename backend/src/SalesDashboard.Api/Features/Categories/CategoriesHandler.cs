using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Metrics;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Categories;

/// <summary>
/// Application logic for the category breakdown block (D8, D9). One EF Core query (D10) LEFT JOINs every
/// category to its Paid line items in the current period, so categories with no sales come back as zeros. The
/// company revenue used for <c>revenueShare</c> is the in-memory sum of the category revenues (identical to a
/// company-wide Paid total), and every derived figure goes through <see cref="MetricMath"/> (D6). Returns an
/// <see cref="ErrorOr{T}"/> so the endpoint owns the HTTP mapping.
/// </summary>
public static class CategoriesHandler
{
    public static async Task<ErrorOr<CategoriesResponse>> HandleAsync(
        PeriodQuery query,
        SalesDbContext db,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(reporting);

        var zone = TimeZoneInfo.FindSystemTimeZoneById(reporting.Value.TimeZone);

        var periodResult = ReportingPeriod.Create(query.From, query.To, zone);
        if (periodResult.IsError)
        {
            return periodResult.Errors;
        }

        var period = periodResult.Value.Current;

        // Two simple queries merged in memory (categories are few): Paid totals grouped by category, and the
        // full category list. The in-memory left join keeps categories with no sales and avoids leaning on a
        // group-join-over-a-subquery translation.
        var sold = await (
            from i in db.SaleItems.AsNoTracking()
            join s in db.Sales on i.SaleId equals s.Id
            where s.Status == SaleStatus.Paid && s.SoldAt >= period.StartUtc && s.SoldAt < period.EndUtc
            join p in db.Products on i.ProductId equals p.Id
            group new { i.LineRevenue, i.LineCost, i.Quantity } by p.CategoryId into g
            select new
            {
                CategoryId = g.Key,
                Revenue = g.Sum(x => x.LineRevenue),
                Cost = g.Sum(x => x.LineCost),
                UnitsSold = g.Sum(x => x.Quantity),
            })
            .ToDictionaryAsync(x => x.CategoryId, cancellationToken)
            .ConfigureAwait(false);

        var categories = await db.Categories.AsNoTracking()
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalRevenue = sold.Values.Sum(x => x.Revenue);

        var items = categories
            .Select(c =>
            {
                var revenue = sold.TryGetValue(c.Id, out var s) ? s.Revenue : 0m;
                var cost = s?.Cost ?? 0m;
                var units = s?.UnitsSold ?? 0;
                return new CategoryRow(
                    CategoryId: c.Id,
                    Name: c.Name,
                    Revenue: revenue,
                    GrossProfit: revenue - cost,
                    Margin: MetricMath.Margin(revenue, cost),
                    UnitsSold: units,
                    RevenueShare: MetricMath.Share(revenue, totalRevenue));
            })
            .OrderByDescending(r => r.Revenue)
            .ToList();

        return new CategoriesResponse(new PeriodDto(period.From, period.To), items);
    }
}
