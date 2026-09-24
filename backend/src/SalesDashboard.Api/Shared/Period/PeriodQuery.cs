using Microsoft.AspNetCore.Mvc;

namespace SalesDashboard.Api.Shared.Period;

/// <summary>
/// Query parameters for a dashboard request (D3). Bound with <c>[AsParameters]</c> so that a missing
/// <c>from</c> or <c>to</c> arrives as <see langword="null"/> and is rejected by our own validation
/// (<see cref="ReportingPeriod.Create"/>), not by the framework's parameter binding.
/// </summary>
public sealed record PeriodQuery
{
    [FromQuery(Name = "from")]
    public DateOnly? From { get; init; }

    [FromQuery(Name = "to")]
    public DateOnly? To { get; init; }
}
