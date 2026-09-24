using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Metrics;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.TopProducts;

/// <summary>
/// Application logic for the top products block (D8, D9). One EF Core query (D10): Paid line items in the
/// period are grouped by product, ranked by gross profit (TOP-PRODUCTS) with revenue then name as tie-breakers,
/// and only the first 10 are read back. Products with no Paid sales never appear. Returns an
/// <see cref="ErrorOr{T}"/> so the endpoint owns the HTTP mapping.
/// </summary>
public static class TopProductsHandler
{
    private const int TopCount = 10;

    public static async Task<ErrorOr<TopProductsResponse>> HandleAsync(
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

        var aggregates =
            from i in db.SaleItems.AsNoTracking()
            join s in db.Sales on i.SaleId equals s.Id
            where s.Status == SaleStatus.Paid && s.SoldAt >= period.StartUtc && s.SoldAt < period.EndUtc
            group new { i.LineRevenue, i.LineCost, i.Quantity } by i.ProductId into g
            select new
            {
                ProductId = g.Key,
                Revenue = g.Sum(x => x.LineRevenue),
                Cost = g.Sum(x => x.LineCost),
                UnitsSold = g.Sum(x => x.Quantity),
            };

        var top =
            from a in aggregates
            join p in db.Products on a.ProductId equals p.Id
            orderby (a.Revenue - a.Cost) descending, a.Revenue descending, p.Name
            select new
            {
                p.Id,
                p.Sku,
                p.Name,
                CategoryName = p.Category!.Name,
                a.Revenue,
                a.Cost,
                a.UnitsSold,
            };

        var rows = await top.Take(TopCount).ToListAsync(cancellationToken).ConfigureAwait(false);

        var items = rows
            .Select(r => new TopProductRow(
                ProductId: r.Id,
                Sku: r.Sku,
                Name: r.Name,
                CategoryName: r.CategoryName,
                Revenue: r.Revenue,
                GrossProfit: r.Revenue - r.Cost,
                Margin: MetricMath.Margin(r.Revenue, r.Cost),
                UnitsSold: r.UnitsSold))
            .ToList();

        return new TopProductsResponse(new PeriodDto(period.From, period.To), items);
    }
}
