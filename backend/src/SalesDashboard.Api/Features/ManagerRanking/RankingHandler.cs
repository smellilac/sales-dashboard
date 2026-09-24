using ErrorOr;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Managers;
using SalesDashboard.Api.Shared.Metrics;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.ManagerRanking;

/// <summary>
/// Application logic for the manager ranking block (D8, D9). One SQL query
/// (<see cref="ManagerPerformanceReader"/>); ordering, sport numbering and visibility are the pure
/// <see cref="CompetitionRanking"/> (D7); the period-over-period change is per metric (RANKING-CHANGE).
/// Returns an <see cref="ErrorOr{T}"/> so the endpoint owns the HTTP mapping.
/// </summary>
public static class RankingHandler
{
    public static async Task<ErrorOr<RankingResponse>> HandleAsync(
        PeriodQuery query,
        string? rankBy,
        SalesDbContext db,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(reporting);

        var metricResult = ParseRankBy(rankBy);
        if (metricResult.IsError)
        {
            return metricResult.Errors;
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(reporting.Value.TimeZone);

        var periodResult = ReportingPeriod.Create(query.From, query.To, zone);
        if (periodResult.IsError)
        {
            return periodResult.Errors;
        }

        var metric = metricResult.Value;
        var period = periodResult.Value;

        var rows = await ManagerPerformanceReader.ReadAsync(db, period, cancellationToken).ConfigureAwait(false);
        var ranked = CompetitionRanking.Rank(rows, metric);

        var items = ranked.Select(r => ToRow(r, metric)).ToList();

        return new RankingResponse(
            RankBy: RankingErrorCodes.ToWire(metric),
            Period: new PeriodDto(period.Current.From, period.Current.To),
            PreviousPeriod: new PeriodDto(period.Previous.From, period.Previous.To),
            Items: items);
    }

    private static ErrorOr<RankingMetric> ParseRankBy(string? rankBy)
    {
        if (string.IsNullOrEmpty(rankBy)
            || string.Equals(rankBy, RankingErrorCodes.GrossProfit, StringComparison.OrdinalIgnoreCase))
        {
            return RankingMetric.GrossProfit;
        }

        if (string.Equals(rankBy, RankingErrorCodes.AverageCheck, StringComparison.OrdinalIgnoreCase))
        {
            return RankingMetric.AverageCheck;
        }

        return Error.Validation(
            code: RankingErrorCodes.InvalidRankBy,
            description: $"'rankBy' must be '{RankingErrorCodes.GrossProfit}' or '{RankingErrorCodes.AverageCheck}'.",
            metadata: new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [PeriodErrorCodes.FieldKey] = RankingErrorCodes.RankByField,
            });
    }

    private static RankingRow ToRow(RankedManager ranked, RankingMetric metric)
    {
        var m = ranked.Manager;
        return new RankingRow(
            Rank: ranked.Rank,
            ManagerId: m.ManagerId,
            FirstName: m.FirstName,
            LastName: m.LastName,
            Team: m.Team,
            IsActive: m.IsActive,
            HasSales: m.HasSales,
            SalesCount: m.CurrentSalesCount,
            Revenue: m.CurrentRevenue,
            GrossProfit: m.CurrentGrossProfit,
            AverageCheck: m.CurrentAverageCheck,
            Margin: m.CurrentMargin,
            MetricChange: MetricChange(m, metric));
    }

    private static decimal? MetricChange(ManagerPerformanceRow manager, RankingMetric metric)
    {
        if (!manager.HasSales)
        {
            return null;
        }

        return manager.CurrentMetric(metric) is { } current && manager.PreviousMetric(metric) is { } previous
            ? MetricMath.RelativeChange(current, previous)
            : null;
    }
}
