namespace SalesDashboard.Api.Shared.Metrics;

/// <summary>
/// A money metric compared to the previous period (API-KPI). <see cref="Change"/> is the relative
/// change (0.12 = +12%), <see langword="null"/> when the previous value is 0 (D6). No rounding (D5).
/// </summary>
public sealed record ValueMetric(decimal Current, decimal Previous, decimal? Change);
