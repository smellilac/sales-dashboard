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
        await AddSaleForProductAsync(managerId, _productId, status, soldAtUtc, quantity: 1, unitPrice: revenue, unitCost: cost);
    }

    /// <summary>A category with the given name.</summary>
    public async Task<Guid> AddCategoryAsync(string name)
    {
        var category = new Category { Id = Guid.CreateVersion7(), Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync(Ct);
        return category.Id;
    }

    /// <summary>A product in <paramref name="categoryId"/>. SKU is unique per call.</summary>
    public async Task<Guid> AddProductAsync(Guid categoryId, string name, decimal listPrice = 100m, decimal baseCost = 40m)
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Sku = "SKU-" + Guid.NewGuid().ToString("N")[..8],
            Name = name,
            CategoryId = categoryId,
            ListPrice = listPrice,
            BaseCost = baseCost,
            VersionAt = VersionAt,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(Ct);
        return product.Id;
    }

    /// <summary>A customer with the given company name.</summary>
    public async Task<Guid> AddCustomerAsync(string company)
    {
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            ContactName = "Contact " + company,
            Company = company,
            Segment = CustomerSegment.Smb,
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(Ct);
        return customer.Id;
    }

    /// <summary>
    /// One sale of <paramref name="productId"/> with a single line: <c>line_revenue = quantity * unitPrice</c>,
    /// <c>line_cost = quantity * unitCost</c>. A shared customer is created lazily unless one is supplied.
    /// </summary>
    public async Task AddSaleForProductAsync(
        Guid managerId,
        Guid productId,
        SaleStatus status,
        DateTime soldAtUtc,
        int quantity,
        decimal unitPrice,
        decimal unitCost,
        Guid? customerId = null)
    {
        await EnsureCustomerAsync();

        var saleId = Guid.CreateVersion7();
        db.Sales.Add(new Sale
        {
            Id = saleId,
            ManagerId = managerId,
            CustomerId = customerId ?? _customerId,
            SoldAt = soldAtUtc,
            Status = status,
        });
        db.SaleItems.Add(new SaleItem
        {
            Id = Guid.CreateVersion7(),
            SaleId = saleId,
            ProductId = productId,
            ProductVersionAt = VersionAt,
            Quantity = quantity,
            UnitPrice = unitPrice,
            UnitCost = unitCost,
        });

        await db.SaveChangesAsync(Ct);
    }

    private async Task EnsureCustomerAsync()
    {
        if (_customerId != Guid.Empty)
        {
            return;
        }

        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            ContactName = "Grace Hopper",
            Company = "Contoso",
            Segment = CustomerSegment.Enterprise,
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(Ct);
        _customerId = customer.Id;
    }

    private async Task EnsureCatalogAsync()
    {
        await EnsureCustomerAsync();

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

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync(Ct);

        _productId = product.Id;
    }
}
