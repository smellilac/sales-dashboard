using Microsoft.EntityFrameworkCore;
using Npgsql;
using SalesDashboard.Api.Data.Entities;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>Schema-level tests against a real PostgreSQL container (migration, model, generated columns, CHECK).</summary>
public sealed class SchemaTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migration_AppliesToEmptyDatabase()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var db = PostgresFixture.CreateContext(connectionString);

        var pending = (await db.Database.GetPendingMigrationsAsync(Ct)).ToList();
        Assert.Contains(pending, m => m.EndsWith("InitialSchema", StringComparison.Ordinal));

        await db.Database.MigrateAsync(Ct);

        var applied = (await db.Database.GetAppliedMigrationsAsync(Ct)).ToList();
        Assert.Contains(applied, m => m.EndsWith("InitialSchema", StringComparison.Ordinal));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
    }

    [Fact]
    public void Model_HasNoPendingChanges()
    {
        using var db = PostgresFixture.CreateContext(postgres.ConnectionString);

        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task GeneratedColumns_ComputeLineRevenueAndLineCost()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var db = PostgresFixture.CreateContext(connectionString);
        await db.Database.MigrateAsync(Ct);

        var saleItemId = await SeedSaleItemAsync(db, quantity: 3, unitPrice: 10.50m, unitCost: 4.00m);

        await using var readDb = PostgresFixture.CreateContext(connectionString);
        var item = await readDb.SaleItems.SingleAsync(i => i.Id == saleItemId, Ct);

        Assert.Equal(31.50m, item.LineRevenue);
        Assert.Equal(12.00m, item.LineCost);
    }

    [Fact]
    public async Task CheckConstraint_RejectsInvalidStatus()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var db = PostgresFixture.CreateContext(connectionString);
        await db.Database.MigrateAsync(Ct);

        var (managerId, customerId) = await SeedManagerAndCustomerAsync(db);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO sales (id, manager_id, customer_id, sold_at, status) " +
            "VALUES (@id, @manager, @customer, @soldAt, @status)";
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("manager", managerId);
        command.Parameters.AddWithValue("customer", customerId);
        command.Parameters.AddWithValue("soldAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("status", "Paidd");

        var exception = await Assert.ThrowsAsync<PostgresException>(
            async () => await command.ExecuteNonQueryAsync(Ct));
        Assert.Equal("23514", exception.SqlState);
    }

    private async Task<(Guid ManagerId, Guid CustomerId)> SeedManagerAndCustomerAsync(Data.SalesDbContext db)
    {
        var manager = new Manager
        {
            Id = Guid.CreateVersion7(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Team = "Alpha",
            Position = "Account Manager",
            IsActive = true,
        };
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            ContactName = "Grace Hopper",
            Company = "Contoso",
            Segment = CustomerSegment.Enterprise,
        };

        db.Managers.Add(manager);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(Ct);

        return (manager.Id, customer.Id);
    }

    private async Task<Guid> SeedSaleItemAsync(Data.SalesDbContext db, int quantity, decimal unitPrice, decimal unitCost)
    {
        var (managerId, customerId) = await SeedManagerAndCustomerAsync(db);

        var versionAt = DateTime.UtcNow;
        var category = new Category { Id = Guid.CreateVersion7(), Name = "Drones" };
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Sku = "SKU-" + Guid.NewGuid().ToString("N")[..8],
            Name = "Mavic",
            CategoryId = category.Id,
            ListPrice = 100.00m,
            BaseCost = 40.00m,
            VersionAt = versionAt,
        };
        var sale = new Sale
        {
            Id = Guid.CreateVersion7(),
            ManagerId = managerId,
            CustomerId = customerId,
            SoldAt = DateTime.UtcNow,
            Status = SaleStatus.Paid,
        };
        var saleItem = new SaleItem
        {
            Id = Guid.CreateVersion7(),
            SaleId = sale.Id,
            ProductId = product.Id,
            ProductVersionAt = versionAt,
            Quantity = quantity,
            UnitPrice = unitPrice,
            UnitCost = unitCost,
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.Sales.Add(sale);
        db.SaleItems.Add(saleItem);
        await db.SaveChangesAsync(Ct);

        return saleItem.Id;
    }
}
