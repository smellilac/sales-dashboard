namespace SalesDashboard.Api.Shared.Metrics;

/// <summary>
/// Shared metric formulas (R4, R6, D6). Each returns <see langword="null"/> for the "no data" case
/// (zero denominator) rather than 0, because "0%" and "nothing to compare" are different facts (D6).
/// No rounding here — rounding happens only on screen (D5).
/// </summary>
public static class MetricMath
{
    /// <summary>Gross margin as a fraction of revenue: <c>(revenue - cost) / revenue</c>.</summary>
    /// <returns><see langword="null"/> when <paramref name="revenue"/> is 0.</returns>
    public static decimal? Margin(decimal revenue, decimal cost) =>
        revenue == 0m ? null : (revenue - cost) / revenue;

    /// <summary>Average check: revenue divided by the number of sales (D2).</summary>
    /// <returns><see langword="null"/> when <paramref name="count"/> is 0.</returns>
    public static decimal? AverageCheck(decimal revenue, int count) =>
        count == 0 ? null : revenue / count;

    /// <summary><paramref name="part"/> as a share of <paramref name="whole"/>, e.g. the refunds rate (D1).</summary>
    /// <returns><see langword="null"/> when <paramref name="whole"/> is 0 (D6).</returns>
    public static decimal? Share(decimal part, decimal whole) =>
        whole == 0m ? null : part / whole;

    /// <summary>Relative change from <paramref name="previous"/> to <paramref name="current"/> (API-KPI).</summary>
    /// <returns><see langword="null"/> when <paramref name="previous"/> is 0 (D6).</returns>
    public static decimal? RelativeChange(decimal current, decimal previous) =>
        previous == 0m ? null : (current - previous) / previous;

    /// <summary>
    /// Change in margin expressed in points (the difference of two fractions), for API-KPI's
    /// <c>changePoints</c>.
    /// </summary>
    /// <returns><see langword="null"/> when either margin is <see langword="null"/> (D6).</returns>
    public static decimal? PointsChange(decimal? currentMargin, decimal? previousMargin) =>
        currentMargin is { } current && previousMargin is { } previous ? current - previous : null;
}
