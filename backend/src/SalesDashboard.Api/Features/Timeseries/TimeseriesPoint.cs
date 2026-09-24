namespace SalesDashboard.Api.Features.Timeseries;

/// <summary>
/// One chart bucket (TIMESERIES). <see cref="BucketStart"/> and <see cref="BucketEnd"/> are inclusive
/// business-zone days clamped to the requested period, so edge buckets carry their real (clipped) dates.
/// All money is Paid only (D2); <see cref="SalesCount"/> counts Paid sales, not line items. No rounding (D5).
/// </summary>
public sealed record TimeseriesPoint(
    DateOnly BucketStart,
    DateOnly BucketEnd,
    decimal Revenue,
    decimal GrossProfit,
    int SalesCount);
