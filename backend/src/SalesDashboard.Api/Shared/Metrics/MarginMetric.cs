namespace SalesDashboard.Api.Shared.Metrics;

/// <summary>
/// A margin metric compared to the previous period (API-KPI). Margin is a fraction and is
/// <see langword="null"/> when there is no revenue (D6). <see cref="ChangePoints"/> is the difference
/// in points, <see langword="null"/> when either margin is <see langword="null"/>. No rounding (D5).
/// </summary>
public sealed record MarginMetric(decimal? Current, decimal? Previous, decimal? ChangePoints);
