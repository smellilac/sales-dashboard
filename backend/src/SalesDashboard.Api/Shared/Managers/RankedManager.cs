namespace SalesDashboard.Api.Shared.Managers;

/// <summary>
/// A manager placed in the ranking (D7). <see cref="Rank"/> is the sport-numbered position (1, 1, 3), or
/// <see langword="null"/> for a manager with no sales in the current period (shown at the bottom).
/// </summary>
public sealed record RankedManager(int? Rank, ManagerPerformanceRow Manager);
