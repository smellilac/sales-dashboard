using SalesDashboard.Api.Shared.Metrics;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Kpi;

/// <summary>
/// The dashboard KPI block (API-KPI). Every figure covers <see cref="Period"/> and is compared to
/// <see cref="PreviousPeriod"/> (D3). Money and count changes are relative; margin change is in points (D6).
/// All totals are Paid only (D2); <see cref="Refunds"/> is the separate Refunded view (D1).
/// </summary>
public sealed record KpiResponse(
    PeriodDto Period,
    PeriodDto PreviousPeriod,
    ValueMetric Revenue,
    ValueMetric GrossProfit,
    CountMetric SalesCount,
    AverageCheckMetric AverageCheck,
    MarginMetric Margin,
    RefundsDto Refunds,
    BestManagerDto? BestManager);
