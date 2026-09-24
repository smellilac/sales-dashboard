using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.TopProducts;

/// <summary>
/// The top products block (TOP-PRODUCTS): up to 10 products ranked by gross profit for <see cref="Period"/>,
/// Paid only (D2). Ties break by revenue descending, then name ascending.
/// </summary>
public sealed record TopProductsResponse(
    PeriodDto Period,
    IReadOnlyList<TopProductRow> Items);
