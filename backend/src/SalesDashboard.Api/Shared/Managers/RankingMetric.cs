namespace SalesDashboard.Api.Shared.Managers;

/// <summary>
/// The metric a manager ranking is ordered by (D7, RANKING-CHANGE). Also selects which value the ranking's
/// period-over-period change is computed from.
/// </summary>
public enum RankingMetric
{
    /// <summary>Gross profit (revenue − cost). The default and the metric used to pick the best manager.</summary>
    GrossProfit,

    /// <summary>Average check: revenue per Paid sale (D2).</summary>
    AverageCheck,
}
