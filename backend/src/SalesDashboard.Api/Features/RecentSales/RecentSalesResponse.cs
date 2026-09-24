namespace SalesDashboard.Api.Features.RecentSales;

/// <summary>
/// A page of the recent-sales feed (RECENT-SALES). <see cref="NextCursor"/> is the opaque keyset cursor for the
/// following page, or <see langword="null"/> on the last page.
/// </summary>
public sealed record RecentSalesResponse(
    IReadOnlyList<RecentSaleRow> Items,
    string? NextCursor);
