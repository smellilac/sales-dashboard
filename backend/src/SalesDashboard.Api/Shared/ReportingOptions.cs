namespace SalesDashboard.Api.Shared;

/// <summary>
/// Options for the <c>Reporting</c> configuration section. <see cref="TimeZone"/> is the business time
/// zone in which days are counted (D4) and in which seed sales are placed. Validated on start.
/// </summary>
public sealed class ReportingOptions
{
    public const string SectionName = "Reporting";

    /// <summary>IANA/Windows time-zone id resolvable by <see cref="TimeZoneInfo.FindSystemTimeZoneById"/>.</summary>
    public string TimeZone { get; init; } = "Europe/Moscow";
}
