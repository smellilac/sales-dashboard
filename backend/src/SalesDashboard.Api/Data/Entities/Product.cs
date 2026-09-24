namespace SalesDashboard.Api.Data.Entities;

/// <summary>
/// A product. <see cref="VersionAt"/> marks the moment the current card (price/cost) took effect (D5);
/// a <see cref="SaleItem"/> copies the price, cost and version as of the sale.
/// </summary>
public sealed class Product
{
    public required Guid Id { get; init; }

    public required string Sku { get; init; }

    public required string Name { get; init; }

    public required Guid CategoryId { get; init; }

    public Category? Category { get; init; }

    public required decimal ListPrice { get; init; }

    public required decimal BaseCost { get; init; }

    public required DateTime VersionAt { get; init; }

    public ICollection<SaleItem> SaleItems { get; init; } = [];
}
