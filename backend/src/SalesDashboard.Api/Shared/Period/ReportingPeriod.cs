using ErrorOr;

namespace SalesDashboard.Api.Shared.Period;

/// <summary>
/// A validated reporting period: the selected window (<see cref="Current"/>) and the window of the
/// same length immediately before it (<see cref="Previous"/>), each with its UTC bounds (D3, D4).
/// </summary>
public sealed class ReportingPeriod
{
    private ReportingPeriod(DateRange current, DateRange previous)
    {
        Current = current;
        Previous = previous;
    }

    /// <summary>The selected window.</summary>
    public DateRange Current { get; }

    /// <summary>The window of the same length ending the day before <see cref="Current"/> starts.</summary>
    public DateRange Previous { get; }

    /// <summary>
    /// Validates <paramref name="from"/>/<paramref name="to"/> and builds the current and previous
    /// windows with UTC bounds computed in <paramref name="zone"/> (D4).
    /// </summary>
    /// <returns>
    /// The period, or a <see cref="ErrorType.Validation"/> error whose <c>Code</c> is one of
    /// <see cref="PeriodErrorCodes"/> and whose metadata carries the offending field name.
    /// </returns>
    public static ErrorOr<ReportingPeriod> Create(DateOnly? from, DateOnly? to, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        if (from is null)
        {
            return RequiredError("from");
        }

        if (to is null)
        {
            return RequiredError("to");
        }

        var fromValue = from.Value;
        var toValue = to.Value;

        if (fromValue > toValue)
        {
            return Error.Validation(
                code: PeriodErrorCodes.FromAfterTo,
                description: "'from' must be on or before 'to'.",
                metadata: Field("from"));
        }

        // Inclusive day count: a single day (from == to) has length 1.
        var lengthDays = (toValue.DayNumber - fromValue.DayNumber) + 1;
        if (lengthDays > PeriodErrorCodes.MaxLengthDays)
        {
            return Error.Validation(
                code: PeriodErrorCodes.TooLong,
                description: $"The period must not exceed {PeriodErrorCodes.MaxLengthDays} days.",
                metadata: Field("to"));
        }

        var current = ToRange(fromValue, toValue, zone);

        // Previous window: same length, ending the day before the current window starts.
        var previousTo = fromValue.AddDays(-1);
        var previousFrom = fromValue.AddDays(-lengthDays);
        var previous = ToRange(previousFrom, previousTo, zone);

        return new ReportingPeriod(current, previous);
    }

    private static DateRange ToRange(DateOnly from, DateOnly to, TimeZoneInfo zone)
    {
        var startUtc = ToUtc(from, zone);
        // Half-open upper bound: the start of the day after the inclusive 'to'.
        var endUtc = ToUtc(to.AddDays(1), zone);
        return new DateRange(from, to, startUtc, endUtc);
    }

    private static DateTime ToUtc(DateOnly day, TimeZoneInfo zone)
    {
        var localMidnight = new DateTime(day, TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, zone);
    }

    private static Error RequiredError(string field) => Error.Validation(
        code: PeriodErrorCodes.Required,
        description: $"'{field}' is required.",
        metadata: Field(field));

    private static Dictionary<string, object> Field(string field) =>
        new(StringComparer.Ordinal) { [PeriodErrorCodes.FieldKey] = field };
}
