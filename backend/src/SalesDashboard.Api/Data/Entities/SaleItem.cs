namespace SalesDashboard.Api.Data.Entities;

/// <summary>
/// A line of a receipt. Stores copies of <see cref="UnitPrice"/>, <see cref="UnitCost"/> and
/// <see cref="ProductVersionAt"/> as of the sale (D5). <see cref="LineRevenue"/> and <see cref="LineCost"/>
/// are STORED generated columns computed by the database.
/// </summary>
public sealed class SaleItem
{
    public required Guid Id { get; init; }

    public required Guid SaleId { get; init; }

    public Sale? Sale { get; init; }

    public required Guid ProductId { get; init; }

    public Product? Product { get; init; }

    /// <summary>The product card version this line was priced against (D5).</summary>
    public required DateTime ProductVersionAt { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public required decimal UnitCost { get; init; }

    /// <summary>Generated column: quantity * unit_price. Computed by the database, never written by the app.</summary>
    public decimal LineRevenue { get; init; }

    /// <summary>Generated column: quantity * unit_cost. Computed by the database, never written by the app.</summary>
    public decimal LineCost { get; init; }
}
