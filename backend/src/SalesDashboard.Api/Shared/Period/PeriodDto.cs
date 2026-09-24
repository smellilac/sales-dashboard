namespace SalesDashboard.Api.Shared.Period;

/// <summary>
/// Wire contract for a period's inclusive day bounds. Returned as <c>period</c> and
/// <c>previousPeriod</c> in dashboard responses (API-KPI).
/// </summary>
public sealed record PeriodDto(DateOnly From, DateOnly To);
