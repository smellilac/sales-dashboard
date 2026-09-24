using SalesDashboard.Api.Shared.Managers;

namespace SalesDashboard.Api.Features.ManagerRanking;

/// <summary>
/// Parsing and error constants for the ranking's <c>rankBy</c> parameter. The wire values are the canonical
/// camelCase names echoed back in the response; an unknown value is a validation error.
/// </summary>
public static class RankingErrorCodes
{
    /// <summary><c>rankBy</c> was not one of the accepted values.</summary>
    public const string InvalidRankBy = "RANKING_INVALID_RANK_BY";

    /// <summary>Query field name carried in the validation error metadata.</summary>
    public const string RankByField = "rankBy";

    /// <summary>Default when <c>rankBy</c> is absent (D7).</summary>
    public const string GrossProfit = "grossProfit";

    /// <summary>Order by average check instead of gross profit.</summary>
    public const string AverageCheck = "averageCheck";

    /// <summary>The canonical wire name of <paramref name="metric"/>, echoed as <c>rankBy</c> in the response.</summary>
    public static string ToWire(RankingMetric metric) => metric switch
    {
        RankingMetric.GrossProfit => GrossProfit,
        RankingMetric.AverageCheck => AverageCheck,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };
}
