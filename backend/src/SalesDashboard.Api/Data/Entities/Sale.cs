namespace SalesDashboard.Api.Data.Entities;

/// <summary>
/// The header of a receipt: manager, customer, date and status. Money lives only in <see cref="SaleItem"/>;
/// status and date live only here.
/// </summary>
public sealed class Sale
{
    public required Guid Id { get; init; }

    public required Guid ManagerId { get; init; }

    public Manager? Manager { get; init; }

    public required Guid CustomerId { get; init; }

    public Customer? Customer { get; init; }

    /// <summary>Instant of the sale, stored as timestamptz in UTC (D4).</summary>
    public required DateTime SoldAt { get; init; }

    public required SaleStatus Status { get; init; }

    public ICollection<SaleItem> Items { get; init; } = [];
}
