namespace SalesDashboard.Api.Features.RecentSales;

/// <summary>One line of a recent sale: what was sold and how many (RECENT-SALES).</summary>
public sealed record RecentSaleItemDto(string ProductName, int Quantity);
