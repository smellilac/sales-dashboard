namespace SalesDashboard.Api.Shared.Metrics;

/// <summary>
/// A count metric (e.g. number of sales) compared to the previous period (API-KPI). Counts are whole
/// numbers, so <see cref="Current"/> and <see cref="Previous"/> are <see cref="int"/>. <see cref="Change"/>
/// is the relative change (0.12 = +12%), <see langword="null"/> when the previous count is 0 (D6).
/// </summary>
public sealed record CountMetric(int Current, int Previous, decimal? Change);
