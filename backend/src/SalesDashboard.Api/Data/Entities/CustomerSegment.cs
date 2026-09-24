namespace SalesDashboard.Api.Data.Entities;

/// <summary>
/// Business segment of a <see cref="Customer"/>.
/// <see cref="Unknown"/> is the zero value: it is the sentinel for an unset segment,
/// so <c>default(CustomerSegment)</c> never silently becomes a meaningful value such as Enterprise.
/// </summary>
public enum CustomerSegment
{
    Unknown = 0,
    Enterprise = 1,
    Smb = 2,
    Government = 3,
}
