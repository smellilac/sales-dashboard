namespace SalesDashboard.Api.Features.Kpi;

/// <summary>
/// The top of the manager ranking by gross profit (D7), shown as the KPI "best manager" card.
/// <see cref="TiedCount"/> is how many <em>other</em> managers share first place; 0 when the winner is alone.
/// </summary>
public sealed record BestManagerDto(
    Guid ManagerId,
    string FirstName,
    string LastName,
    decimal GrossProfit,
    int TiedCount);
