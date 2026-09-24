namespace SalesDashboard.Api.Features.RecentSales;

/// <summary>
/// Validation constants for the recent-sales feed (RECENT-SALES). Codes are stable SCREAMING_SNAKE_CASE
/// strings surfaced to the client; the offending field travels in the error metadata.
/// </summary>
public static class RecentSalesErrorCodes
{
    /// <summary><c>cursor</c> was not a well-formed opaque cursor.</summary>
    public const string InvalidCursor = "RECENT_SALES_INVALID_CURSOR";

    /// <summary><c>limit</c> was outside the accepted 1..100 range.</summary>
    public const string InvalidLimit = "RECENT_SALES_INVALID_LIMIT";

    /// <summary>Query field name for a bad cursor, carried in the validation error metadata.</summary>
    public const string CursorField = "cursor";

    /// <summary>Query field name for a bad limit, carried in the validation error metadata.</summary>
    public const string LimitField = "limit";

    /// <summary>Default page size when <c>limit</c> is absent (RECENT-SALES).</summary>
    public const int DefaultLimit = 20;

    /// <summary>Smallest accepted page size.</summary>
    public const int MinLimit = 1;

    /// <summary>Largest accepted page size (RECENT-SALES).</summary>
    public const int MaxLimit = 100;
}
