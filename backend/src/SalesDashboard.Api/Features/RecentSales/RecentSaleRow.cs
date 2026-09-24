using SalesDashboard.Api.Data.Entities;

namespace SalesDashboard.Api.Features.RecentSales;

/// <summary>
/// One row of the recent-sales feed (RECENT-SALES). All statuses appear; <see cref="Amount"/> and
/// <see cref="GrossProfit"/> are the sale's line totals for every status (Cancelled/Refunded shown greyed and
/// excluded from headline totals by the frontend, D2). <see cref="SoldAt"/> is UTC (D4). No rounding (D5).
/// </summary>
public sealed record RecentSaleRow(
    Guid SaleId,
    DateTime SoldAt,
    Guid ManagerId,
    string ManagerName,
    string CustomerCompany,
    SaleStatus Status,
    decimal Amount,
    decimal GrossProfit,
    IReadOnlyList<RecentSaleItemDto> Items);
