using SalesDashboard.Api.Shared.Metrics;

namespace SalesDashboard.Api.Shared.Managers;

/// <summary>
/// One manager's raw Paid aggregates for the current and previous period (D2), as read in a single query by
/// <see cref="ManagerPerformanceReader"/>. Only sums live here (no rounding, D5); every derived figure goes
/// through <see cref="MetricMath"/> so the "no data → null" rule (D6) lives in one place. Shared by the KPI
/// best-manager card and the ranking (D7).
/// </summary>
public sealed record ManagerPerformanceRow
{
    public required Guid ManagerId { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Team { get; init; }

    public required bool IsActive { get; init; }

    public required decimal CurrentRevenue { get; init; }

    public required decimal CurrentCost { get; init; }

    public required int CurrentSalesCount { get; init; }

    public required decimal PreviousRevenue { get; init; }

    public required decimal PreviousCost { get; init; }

    public required int PreviousSalesCount { get; init; }

    /// <summary>Whether the manager had any Paid sale in the current period (D7 visibility/ordering).</summary>
    public bool HasSales => CurrentSalesCount > 0;

    public decimal CurrentGrossProfit => CurrentRevenue - CurrentCost;

    public decimal PreviousGrossProfit => PreviousRevenue - PreviousCost;

    public decimal? CurrentAverageCheck => MetricMath.AverageCheck(CurrentRevenue, CurrentSalesCount);

    public decimal? PreviousAverageCheck => MetricMath.AverageCheck(PreviousRevenue, PreviousSalesCount);

    public decimal? CurrentMargin => MetricMath.Margin(CurrentRevenue, CurrentCost);

    /// <summary>The current-period value of <paramref name="metric"/>, used for ordering and change.</summary>
    public decimal? CurrentMetric(RankingMetric metric) => metric switch
    {
        RankingMetric.GrossProfit => CurrentGrossProfit,
        RankingMetric.AverageCheck => CurrentAverageCheck,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };

    /// <summary>The previous-period value of <paramref name="metric"/>, used for the change (RANKING-CHANGE).</summary>
    public decimal? PreviousMetric(RankingMetric metric) => metric switch
    {
        RankingMetric.GrossProfit => PreviousGrossProfit,
        RankingMetric.AverageCheck => PreviousAverageCheck,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };
}
