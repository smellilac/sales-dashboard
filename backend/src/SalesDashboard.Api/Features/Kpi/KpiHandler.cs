using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Managers;
using SalesDashboard.Api.Shared.Metrics;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Kpi;

/// <summary>
/// Application logic for the KPI block (D8, D9). Two SQL queries — one for per-manager Paid aggregates
/// (company totals are their in-memory sum, and the best manager reuses <see cref="CompetitionRanking"/>) and
/// one for Refunded revenue (D1). Returns an <see cref="ErrorOr{T}"/> so the endpoint owns the HTTP mapping.
/// </summary>
public static class KpiHandler
{
    public static async Task<ErrorOr<KpiResponse>> HandleAsync(
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

        var period = periodResult.Value;

        var rows = await ManagerPerformanceReader.ReadAsync(db, period, cancellationToken).ConfigureAwait(false);

        // Company totals = sum of the per-manager Paid aggregates (identical to a company-wide Paid aggregate),
        // which keeps the endpoint at two queries.
        var currentRevenue = rows.Sum(r => r.CurrentRevenue);
        var currentCost = rows.Sum(r => r.CurrentCost);
        var currentCount = rows.Sum(r => r.CurrentSalesCount);
        var previousRevenue = rows.Sum(r => r.PreviousRevenue);
        var previousCost = rows.Sum(r => r.PreviousCost);
        var previousCount = rows.Sum(r => r.PreviousSalesCount);

        var (currentRefund, previousRefund) = await ReadRefundsAsync(db, period, cancellationToken)
            .ConfigureAwait(false);

        return new KpiResponse(
            Period: new PeriodDto(period.Current.From, period.Current.To),
            PreviousPeriod: new PeriodDto(period.Previous.From, period.Previous.To),
            Revenue: Value(currentRevenue, previousRevenue),
            GrossProfit: Value(currentRevenue - currentCost, previousRevenue - previousCost),
            SalesCount: Count(currentCount, previousCount),
            AverageCheck: AverageCheck(currentRevenue, currentCount, previousRevenue, previousCount),
            Margin: Margin(currentRevenue, currentCost, previousRevenue, previousCost),
            Refunds: Refunds(currentRefund, previousRefund, currentRevenue, previousRevenue),
            BestManager: BestManager(rows));
    }

    /// <summary>Refunded revenue for the current and previous period in one query (D1), by sale date (D4).</summary>
    private static async Task<(decimal Current, decimal Previous)> ReadRefundsAsync(
        SalesDbContext db,
        ReportingPeriod period,
        CancellationToken cancellationToken)
    {
        var spanStart = period.Previous.StartUtc;
        var spanEnd = period.Current.EndUtc;
        var split = period.Current.StartUtc;

        var saleMoney =
            from i in db.SaleItems.AsNoTracking()
            group i by i.SaleId into g
            select new { SaleId = g.Key, Revenue = g.Sum(x => x.LineRevenue) };

        var refundLines =
            from s in db.Sales.AsNoTracking()
            where s.Status == SaleStatus.Refunded && s.SoldAt >= spanStart && s.SoldAt < spanEnd
            join money in saleMoney on s.Id equals money.SaleId
            select new { IsCurrent = s.SoldAt >= split, money.Revenue };

        var totals = await refundLines
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Current = g.Sum(x => x.IsCurrent ? x.Revenue : 0m),
                Previous = g.Sum(x => x.IsCurrent ? 0m : x.Revenue),
            })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return (totals?.Current ?? 0m, totals?.Previous ?? 0m);
    }

    private static ValueMetric Value(decimal current, decimal previous) =>
        new(current, previous, MetricMath.RelativeChange(current, previous));

    private static CountMetric Count(int current, int previous) =>
        new(current, previous, MetricMath.RelativeChange(current, previous));

    private static AverageCheckMetric AverageCheck(
        decimal currentRevenue, int currentCount, decimal previousRevenue, int previousCount)
    {
        var current = MetricMath.AverageCheck(currentRevenue, currentCount);
        var previous = MetricMath.AverageCheck(previousRevenue, previousCount);
        var change = current is { } c && previous is { } p ? MetricMath.RelativeChange(c, p) : null;
        return new AverageCheckMetric(current, previous, change);
    }

    private static MarginMetric Margin(
        decimal currentRevenue, decimal currentCost, decimal previousRevenue, decimal previousCost)
    {
        var current = MetricMath.Margin(currentRevenue, currentCost);
        var previous = MetricMath.Margin(previousRevenue, previousCost);
        return new MarginMetric(current, previous, MetricMath.PointsChange(current, previous));
    }

    private static RefundsDto Refunds(
        decimal currentRefund, decimal previousRefund, decimal currentPaid, decimal previousPaid)
    {
        var currentRate = MetricMath.Share(currentRefund, currentPaid + currentRefund);
        var previousRate = MetricMath.Share(previousRefund, previousPaid + previousRefund);
        return new RefundsDto(
            Value(currentRefund, previousRefund),
            new MarginMetric(currentRate, previousRate, MetricMath.PointsChange(currentRate, previousRate)));
    }

    private static BestManagerDto? BestManager(IReadOnlyList<ManagerPerformanceRow> rows)
    {
        var ranked = CompetitionRanking.Rank(rows, RankingMetric.GrossProfit);
        var leaders = ranked.Where(static r => r.Rank == 1).ToList();
        if (leaders.Count == 0)
        {
            return null;
        }

        var winner = leaders[0].Manager;
        return new BestManagerDto(
            winner.ManagerId,
            winner.FirstName,
            winner.LastName,
            winner.CurrentGrossProfit,
            leaders.Count - 1);
    }
}
