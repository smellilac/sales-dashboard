namespace SalesDashboard.Api.Features.Timeseries;

/// <summary>
/// The bucket size the server picks for the revenue chart (TIMESERIES). The choice is a function of the
/// period length only, so the frontend never has to ask for a step and cannot request 730 daily points.
/// </summary>
public enum TimeseriesGranularity
{
    /// <summary>One bucket per calendar day (period length ≤ 31 days).</summary>
    Day,

    /// <summary>One bucket per ISO week starting Monday (period length ≤ 180 days).</summary>
    Week,

    /// <summary>One bucket per calendar month (longer periods).</summary>
    Month,
}
