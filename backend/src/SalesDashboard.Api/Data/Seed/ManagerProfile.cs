namespace SalesDashboard.Api.Data.Seed;

/// <summary>
/// One of the 20 seed managers. <see cref="FrequencyWeight"/> is the relative chance of being picked for a
/// sale; discount, line-count, quantity and cancel/refund shares shape the sales. <see cref="ManagerProfileKind.Vacation"/>
/// and <see cref="ManagerProfileKind.Terminated"/> get their sale-free windows from the generator (relative
/// to the run day).
/// </summary>
internal sealed record ManagerProfile(
    ManagerProfileKind Kind,
    string Team,
    string Position,
    bool IsActive,
    double FrequencyWeight,
    decimal DiscountMin,
    decimal DiscountMax,
    int LineItemsMin,
    int LineItemsMax,
    int QuantityMin,
    int QuantityMax,
    double CancelledShare,
    double RefundedShare)
{
    public const string TeamEnterprise = "Enterprise";
    public const string TeamAgro = "Агро";
    public const string TeamRetail = "Розница";
    public const string TeamGovernment = "Госсектор";

    /// <summary>The 20 profiles in a fixed order; the generator zips faker-generated names onto them.</summary>
    public static IReadOnlyList<ManagerProfile> All { get; } =
    [
        // 3 strong: frequent, small discounts, clean.
        Strong(TeamEnterprise),
        Strong(TeamAgro),
        Strong(TeamRetail),

        // 3 weak: infrequent, deep discounts, more cancellations and refunds.
        Weak(TeamRetail),
        Weak(TeamGovernment),
        Weak(TeamAgro),

        // 2 big deals: rare but large.
        BigDeals(TeamEnterprise),
        BigDeals(TeamGovernment),

        // 2 many small: very frequent, tiny tickets.
        ManySmall(TeamRetail),
        ManySmall(TeamRetail),

        // 1 on a 6-week vacation, 1 terminated 3 months ago (inactive, no recent sales).
        new(ManagerProfileKind.Vacation, TeamAgro, "Менеджер по продажам", IsActive: true,
            FrequencyWeight: 1.0, 0.03m, 0.10m, 1, 3, 1, 3, CancelledShare: 0.06, RefundedShare: 0.04),
        new(ManagerProfileKind.Terminated, TeamGovernment, "Менеджер по продажам", IsActive: false,
            FrequencyWeight: 1.0, 0.03m, 0.10m, 1, 3, 1, 3, CancelledShare: 0.07, RefundedShare: 0.04),

        // 8 average.
        Average(TeamEnterprise),
        Average(TeamEnterprise),
        Average(TeamAgro),
        Average(TeamRetail),
        Average(TeamRetail),
        Average(TeamGovernment),
        Average(TeamGovernment),
        Average(TeamAgro),
    ];

    private static ManagerProfile Strong(string team) => new(
        ManagerProfileKind.Strong, team, "Ведущий менеджер", IsActive: true,
        FrequencyWeight: 2.0, 0.00m, 0.05m, 1, 4, 1, 3, CancelledShare: 0.03, RefundedShare: 0.02);

    private static ManagerProfile Weak(string team) => new(
        ManagerProfileKind.Weak, team, "Младший менеджер", IsActive: true,
        FrequencyWeight: 0.6, 0.05m, 0.15m, 1, 2, 1, 2, CancelledShare: 0.11, RefundedShare: 0.06);

    private static ManagerProfile BigDeals(string team) => new(
        ManagerProfileKind.BigDeals, team, "Старший менеджер", IsActive: true,
        FrequencyWeight: 0.5, 0.02m, 0.08m, 2, 4, 3, 8, CancelledShare: 0.06, RefundedShare: 0.04);

    private static ManagerProfile ManySmall(string team) => new(
        ManagerProfileKind.ManySmall, team, "Менеджер по продажам", IsActive: true,
        FrequencyWeight: 2.5, 0.03m, 0.10m, 1, 2, 1, 2, CancelledShare: 0.06, RefundedShare: 0.04);

    private static ManagerProfile Average(string team) => new(
        ManagerProfileKind.Average, team, "Менеджер по продажам", IsActive: true,
        FrequencyWeight: 1.0, 0.02m, 0.10m, 1, 4, 1, 3, CancelledShare: 0.06, RefundedShare: 0.04);
}
