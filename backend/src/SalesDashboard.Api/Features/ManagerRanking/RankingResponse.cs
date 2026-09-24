using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.ManagerRanking;

/// <summary>
/// The manager ranking block (D7, RANKING-CHANGE). <see cref="RankBy"/> echoes the metric the rows are ordered
/// by; each row's change is measured against <see cref="PreviousPeriod"/> (D3).
/// </summary>
public sealed record RankingResponse(
    string RankBy,
    PeriodDto Period,
    PeriodDto PreviousPeriod,
    IReadOnlyList<RankingRow> Items);
