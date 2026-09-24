using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Timeseries;

/// <summary>Server-side step selection and its <c>date_trunc</c>/wire names (TIMESERIES).</summary>
public static class TimeseriesGranularitySelector
{
    private const int DailyUpToDays = 31;
    private const int WeeklyUpToDays = 180;

    /// <summary>Picks the granularity for <paramref name="current"/> from its inclusive day length (TIMESERIES).</summary>
    public static TimeseriesGranularity ForPeriod(DateRange current)
    {
        var lengthDays = (current.To.DayNumber - current.From.DayNumber) + 1;

        return lengthDays switch
        {
            <= DailyUpToDays => TimeseriesGranularity.Day,
            <= WeeklyUpToDays => TimeseriesGranularity.Week,
            _ => TimeseriesGranularity.Month,
        };
    }

    /// <summary>The PostgreSQL <c>date_trunc</c> unit, also the value passed as the SQL <c>@step</c> parameter.</summary>
    public static string ToTruncField(this TimeseriesGranularity granularity) => granularity switch
    {
        TimeseriesGranularity.Day => "day",
        TimeseriesGranularity.Week => "week",
        TimeseriesGranularity.Month => "month",
        _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, null),
    };

    /// <summary>The camelCase name echoed as <c>granularity</c> in the response.</summary>
    public static string ToWire(this TimeseriesGranularity granularity) => granularity.ToTruncField();
}
