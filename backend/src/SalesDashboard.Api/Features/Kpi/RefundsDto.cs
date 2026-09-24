using SalesDashboard.Api.Shared.Metrics;

namespace SalesDashboard.Api.Features.Kpi;

/// <summary>
/// The refunds KPI card (D1). <see cref="Amount"/> is the Refunded revenue by sale date; <see cref="Rate"/>
/// is its share of Paid + Refunded revenue (a fraction, so a <see cref="MarginMetric"/> with change in points).
/// </summary>
public sealed record RefundsDto(ValueMetric Amount, MarginMetric Rate);
