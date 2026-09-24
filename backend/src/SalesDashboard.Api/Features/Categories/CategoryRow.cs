namespace SalesDashboard.Api.Features.Categories;

/// <summary>
/// One category's Paid totals for the period (CAT-COUNT). No sales count — one sale can span categories, so a
/// per-category count would exceed the company total. <see cref="Margin"/> is <see langword="null"/> when the
/// category has no revenue, and <see cref="RevenueShare"/> is <see langword="null"/> when company revenue is 0
/// (D6). Categories with no sales appear with zeros. No rounding (D5).
/// </summary>
public sealed record CategoryRow(
    Guid CategoryId,
    string Name,
    decimal Revenue,
    decimal GrossProfit,
    decimal? Margin,
    int UnitsSold,
    decimal? RevenueShare);
