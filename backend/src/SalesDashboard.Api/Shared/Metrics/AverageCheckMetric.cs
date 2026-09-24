namespace SalesDashboard.Api.Shared.Metrics;

/// <summary>
/// The average check compared to the previous period (API-KPI, D2). Unlike <see cref="ValueMetric"/> the
/// current and previous values are nullable: the average check is <see langword="null"/> when there are no
/// sales (D6, UI "—"), a distinct fact from 0. <see cref="Change"/> is the relative change (0.12 = +12%),
/// <see langword="null"/> when the previous value is 0 or absent. No rounding (D5).
/// </summary>
public sealed record AverageCheckMetric(decimal? Current, decimal? Previous, decimal? Change);
