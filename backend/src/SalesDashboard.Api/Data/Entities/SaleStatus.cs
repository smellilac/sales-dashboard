namespace SalesDashboard.Api.Data.Entities;

/// <summary>
/// Lifecycle status of a <see cref="Sale"/>.
/// <see cref="Unknown"/> is the zero value: it is the sentinel for an unset status,
/// so <c>default(SaleStatus)</c> never silently becomes a meaningful business value (D1, D2).
/// </summary>
public enum SaleStatus
{
    Unknown = 0,
    Paid = 1,
    Cancelled = 2,
    Refunded = 3,
}
