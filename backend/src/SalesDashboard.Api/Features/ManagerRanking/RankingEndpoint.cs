using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Managers;
using SalesDashboard.Api.Shared.Metrics;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.ManagerRanking;

/// <summary>
/// Manager ranking slice (D8, D9): <c>GET /api/dashboard/managers/ranking</c>. One SQL query
/// (<see cref="ManagerPerformanceReader"/>); ordering, sport numbering and visibility are the pure
/// <see cref="CompetitionRanking"/> (D7). The period-over-period change is per metric (RANKING-CHANGE).
/// </summary>
public static class RankingEndpoint
{
    public static RouteGroupBuilder MapManagerRanking(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/managers/ranking", HandleAsync)
            .WithName("GetManagerRanking")
            .WithSummary("Managers ranked by gross profit or average check for the selected period.")
            .Produces<RankingResponse>()
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] PeriodQuery query,
        [FromQuery] string? rankBy,
        SalesDbContext db,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        var metricResult = ParseRankBy(rankBy);
        if (metricResult.IsError)
        {
            return metricResult.Errors.ToProblemResult();
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(reporting.Value.TimeZone);

        var periodResult = ReportingPeriod.Create(query.From, query.To, zone);
        if (periodResult.IsError)
        {
            return periodResult.Errors.ToProblemResult();
        }

        var metric = metricResult.Value;
        var period = periodResult.Value;

        var rows = await ManagerPerformanceReader.ReadAsync(db, period, cancellationToken).ConfigureAwait(false);
        var ranked = CompetitionRanking.Rank(rows, metric);

        var items = ranked.Select(r => ToRow(r, metric)).ToList();

        var response = new RankingResponse(
            RankBy: RankingErrorCodes.ToWire(metric),
            Period: new PeriodDto(period.Current.From, period.Current.To),
            PreviousPeriod: new PeriodDto(period.Previous.From, period.Previous.To),
            Items: items);

        return Results.Ok(response);
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
