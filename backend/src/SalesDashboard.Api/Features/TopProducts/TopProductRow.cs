namespace SalesDashboard.Api.Features.TopProducts;

/// <summary>
/// One product in the top-10 by gross profit (TOP-PRODUCTS). <see cref="Margin"/> is <see langword="null"/>
/// only when the product has no revenue (D6) — it cannot happen here, since ranked products always have Paid
/// sales, but the shared formula keeps the rule in one place. No rounding (D5).
/// </summary>
public sealed record TopProductRow(
    Guid ProductId,
    string Sku,
    string Name,
    string CategoryName,
    decimal Revenue,
    decimal GrossProfit,
    decimal? Margin,
    int UnitsSold);
