namespace SalesDashboard.Api.Shared.Managers;

/// <summary>
/// Turns raw manager performance into a ranking with sport numbering (D7). Pure and deterministic: no I/O,
/// order depends only on the input. Rules:
/// <list type="bullet">
/// <item>Managers with Paid sales in the current period are ranked by the chosen metric, descending. Sport
/// numbering (1, 1, 3): a tie is equal <em>metric value</em> only; within a tie, order is revenue ↓ then
/// last name, first name ↑ — but tied rows keep the same rank.</item>
/// <item>Managers with no current sales come last with <see cref="RankedManager.Rank"/> = <see langword="null"/>,
/// ordered by name. Inactive managers with no sales are hidden; inactive managers with sales are ranked
/// normally (their <c>IsActive = false</c> travels through).</item>
/// </list>
/// </summary>
public static class CompetitionRanking
{
    public static IReadOnlyList<RankedManager> Rank(
        IEnumerable<ManagerPerformanceRow> rows,
        RankingMetric metric)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var ranked = new List<RankedManager>();

        var withSales = rows
            .Where(static row => row.HasSales)
            .OrderByDescending(row => row.CurrentMetric(metric))
            .ThenByDescending(static row => row.CurrentRevenue)
            .ThenBy(static row => row.LastName, StringComparer.Ordinal)
            .ThenBy(static row => row.FirstName, StringComparer.Ordinal)
            .ToList();

        var position = 0;
        decimal? previousMetric = null;
        var rank = 0;
        foreach (var row in withSales)
        {
            position++;
            var value = row.CurrentMetric(metric);
            // Sport numbering: only advance the rank when the metric value actually changes.
            if (rank == 0 || value != previousMetric)
            {
                rank = position;
                previousMetric = value;
            }

            ranked.Add(new RankedManager(rank, row));
        }

        // Active managers with no current sales: shown at the bottom, no rank, by name.
        // Inactive managers with no current sales are hidden (skipped entirely).
        var withoutSales = rows
            .Where(static row => !row.HasSales && row.IsActive)
            .OrderBy(static row => row.LastName, StringComparer.Ordinal)
            .ThenBy(static row => row.FirstName, StringComparer.Ordinal);

        foreach (var row in withoutSales)
        {
            ranked.Add(new RankedManager(null, row));
        }

        return ranked;
    }
}
