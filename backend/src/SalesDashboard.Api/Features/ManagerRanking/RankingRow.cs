namespace SalesDashboard.Api.Features.ManagerRanking;

/// <summary>
/// One row of the manager ranking (D7). <see cref="Rank"/> is the sport-numbered place (1, 1, 3) or
/// <see langword="null"/> for a manager with no sales in the current period. <see cref="AverageCheck"/> and
/// <see cref="Margin"/> are <see langword="null"/> when there are no sales (D6). <see cref="MetricChange"/> is
/// the relative change of the ranked-by metric versus the previous period (RANKING-CHANGE), <see langword="null"/>
/// when the previous value is 0 or the manager has no current sales.
/// </summary>
public sealed record RankingRow(
    int? Rank,
    Guid ManagerId,
    string FirstName,
    string LastName,
    string Team,
    bool IsActive,
    bool HasSales,
    int SalesCount,
    decimal Revenue,
    decimal GrossProfit,
    decimal? AverageCheck,
    decimal? Margin,
    decimal? MetricChange);
