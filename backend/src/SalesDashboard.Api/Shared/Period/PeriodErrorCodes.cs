namespace SalesDashboard.Api.Shared.Period;

/// <summary>
/// Validation error codes for the reporting period (D3, D4). Codes are stable SCREAMING_SNAKE_CASE
/// strings surfaced to the client in the problem response; the offending field name travels in
/// <c>Error.Metadata[<see cref="FieldKey"/>]</c>.
/// </summary>
public static class PeriodErrorCodes
{
    /// <summary><c>from</c> or <c>to</c> was not supplied.</summary>
    public const string Required = "PERIOD_REQUIRED";

    /// <summary><c>from</c> is later than <c>to</c>.</summary>
    public const string FromAfterTo = "PERIOD_FROM_AFTER_TO";

    /// <summary>The inclusive range spans more than <see cref="MaxLengthDays"/> days.</summary>
    public const string TooLong = "PERIOD_TOO_LONG";

    /// <summary>Metadata key carrying the offending field name (<c>from</c> or <c>to</c>).</summary>
    public const string FieldKey = "field";

    /// <summary>Maximum inclusive length of a period: two years (D4).</summary>
    public const int MaxLengthDays = 731;
}
