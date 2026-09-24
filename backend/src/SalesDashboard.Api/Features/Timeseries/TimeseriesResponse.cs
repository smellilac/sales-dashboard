using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Timeseries;

/// <summary>
/// The revenue-over-time block (TIMESERIES). <see cref="Granularity"/> (<c>day</c>/<c>week</c>/<c>month</c>)
/// is chosen by the server from the length of <see cref="Period"/>; <see cref="Points"/> covers the whole
/// period with empty buckets filled as zeros.
/// </summary>
public sealed record TimeseriesResponse(
    string Granularity,
    PeriodDto Period,
    IReadOnlyList<TimeseriesPoint> Points);
