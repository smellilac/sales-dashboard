namespace SalesDashboard.Api.Data.Entities;

/// <summary>A product category.</summary>
public sealed class Category
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public ICollection<Product> Products { get; init; } = [];
}
