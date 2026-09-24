namespace SalesDashboard.Api.Data.Entities;

/// <summary>A B2B customer company that buys through a <see cref="Manager"/>.</summary>
public sealed class Customer
{
    public required Guid Id { get; init; }

    public required string ContactName { get; init; }

    public required string Company { get; init; }

    public required CustomerSegment Segment { get; init; }

    public ICollection<Sale> Sales { get; init; } = [];
}
