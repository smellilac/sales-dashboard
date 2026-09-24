using SalesDashboard.Api.Data;
using SalesDashboard.Api.Data.Entities;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>
/// Short, explicit test data for the dashboard slices — managers and their sales, built by hand (no seed) so
/// each test controls exactly what falls into which period (D2, D4). A sale is one line whose
/// <c>line_revenue</c>/<c>line_cost</c> (generated columns) equal the supplied revenue/cost, keeping money
/// assertions obvious. A single shared customer/product/category is created lazily.
/// </summary>
internal sealed class TestDataBuilder(SalesDbContext db)
{
    private static readonly DateTime VersionAt = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Guid _customerId;
    private Guid _productId;

    public async Task<Guid> AddManagerAsync(
        string firstName,
        string lastName,
        bool isActive = true,
        string team = "Alpha")
    {
        var manager = new Manager
        {
            Id = Guid.CreateVersion7(),
            FirstName = firstName,
            LastName = lastName,
            Team = team,
            Position = "Account Manager",
            IsActive = isActive,
        };

        db.Managers.Add(manager);
        await db.SaveChangesAsync(Ct);
        return manager.Id;
    }

    /// <summary>Adds one sale with a single line: <c>line_revenue = revenue</c>, <c>line_cost = cost</c>.</summary>
    public async Task AddSaleAsync(
        Guid managerId,
        SaleStatus status,
        DateTime soldAtUtc,
        decimal revenue,
        decimal cost)
    {
        await EnsureCatalogAsync();

        var saleId = Guid.CreateVersion7();
        db.Sales.Add(new Sale
        {
            Id = saleId,
            ManagerId = managerId,
            CustomerId = _customerId,
            SoldAt = soldAtUtc,
            Status = status,
        });
        db.SaleItems.Add(new SaleItem
        {
            Id = Guid.CreateVersion7(),
            SaleId = saleId,
            ProductId = _productId,
            ProductVersionAt = VersionAt,
            Quantity = 1,
            UnitPrice = revenue,
            UnitCost = cost,
        });

        await db.SaveChangesAsync(Ct);
    }

    private async Task EnsureCatalogAsync()
    {
        if (_productId != Guid.Empty)
        {
            return;
        }

        var category = new Category { Id = Guid.CreateVersion7(), Name = "Drones" };
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Sku = "SKU-" + Guid.NewGuid().ToString("N")[..8],
            Name = "Mavic",
            CategoryId = category.Id,
            ListPrice = 100.00m,
            BaseCost = 40.00m,
            VersionAt = VersionAt,
        };
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            ContactName = "Grace Hopper",
            Company = "Contoso",
            Segment = CustomerSegment.Enterprise,
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(Ct);

        _customerId = customer.Id;
        _productId = product.Id;
    }
}
