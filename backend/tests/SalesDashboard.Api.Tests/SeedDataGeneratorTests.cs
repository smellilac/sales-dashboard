using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Data.Seed;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>Pure (DB-free) tests for the deterministic seed generator (D13).</summary>
public sealed class SeedDataGeneratorTests
{
    private static readonly TimeZoneInfo Moscow = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.FromHours(3));

    private static SeedDataGenerator.SeedData Generate() => SeedDataGenerator.Generate(Now, Moscow);

    [Fact]
    public void Generate_IsDeterministic_IncludingIds()
    {
        var first = Generate();
        var second = Generate();

        Assert.Equal(first.Managers.Select(m => m.Id), second.Managers.Select(m => m.Id));
        Assert.Equal(first.Customers.Select(c => c.Id), second.Customers.Select(c => c.Id));
        Assert.Equal(first.Products.Select(p => (p.Id, p.Sku, p.ListPrice, p.BaseCost)), second.Products.Select(p => (p.Id, p.Sku, p.ListPrice, p.BaseCost)));
        Assert.Equal(first.Sales.Select(s => (s.Id, s.SoldAt, s.Status, s.ManagerId, s.CustomerId)), second.Sales.Select(s => (s.Id, s.SoldAt, s.Status, s.ManagerId, s.CustomerId)));
        Assert.Equal(first.SaleItems.Select(i => (i.Id, i.SaleId, i.ProductId, i.Quantity, i.UnitPrice, i.UnitCost)), second.SaleItems.Select(i => (i.Id, i.SaleId, i.ProductId, i.Quantity, i.UnitPrice, i.UnitCost)));
    }

    [Fact]
    public void Generate_ProducesExpectedVolumes()
    {
        var data = Generate();

        Assert.Equal(20, data.Managers.Count);
        Assert.Equal(80, data.Customers.Count);
        Assert.Equal(6, data.Categories.Count);
        Assert.InRange(data.Products.Count, 35, 45);
        Assert.InRange(data.Sales.Count, 3500, 4500);

        var itemsPerSale = data.SaleItems.GroupBy(i => i.SaleId).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(data.Sales.Count, itemsPerSale.Count); // every sale has at least one item
        Assert.All(itemsPerSale.Values, count => Assert.InRange(count, 1, 4));
    }

    [Fact]
    public void Generate_CustomerSegments_MatchTargetDistribution()
    {
        var data = Generate();

        Assert.Equal(20, data.Customers.Count(c => c.Segment == CustomerSegment.Enterprise));
        Assert.Equal(48, data.Customers.Count(c => c.Segment == CustomerSegment.Smb));
        Assert.Equal(12, data.Customers.Count(c => c.Segment == CustomerSegment.Government));
    }

    [Fact]
    public void Generate_ProductPrices_StayWithinCatalogBands()
    {
        var data = Generate();
        var byName = data.Products.ToDictionary(p => p.Name, StringComparer.Ordinal);

        foreach (var def in SeedCatalog.Products)
        {
            var product = byName[def.Name];
            // The latest list price may be up to +15% above the band after a mid-year bump.
            Assert.InRange(product.ListPrice, def.PriceMin, def.PriceMax * 1.15m);
            Assert.InRange(product.BaseCost, 0m, product.ListPrice);
        }
    }

    [Fact]
    public void Generate_StatusShares_AreWithinExpectedRange()
    {
        var data = Generate();
        double total = data.Sales.Count;

        var cancelled = data.Sales.Count(s => s.Status == SaleStatus.Cancelled) / total;
        var refunded = data.Sales.Count(s => s.Status == SaleStatus.Refunded) / total;

        Assert.InRange(cancelled, 0.04, 0.09); // spec 5-8%, tolerance for the profile mix
        Assert.InRange(refunded, 0.025, 0.06); // spec 3-5%
    }

    [Fact]
    public void Generate_VacationManager_HasNoSalesInVacationWindow()
    {
        var data = Generate();
        var vacation = data.Managers[10]; // fixed profile order: index 10 is the vacation profile
        var start = Now.AddDays(-120).UtcDateTime;
        var end = start.AddDays(42);

        var salesInWindow = data.Sales
            .Where(s => s.ManagerId == vacation.Id)
            .Count(s => s.SoldAt >= start && s.SoldAt < end);

        Assert.Equal(0, salesInWindow);
        Assert.Contains(data.Sales, s => s.ManagerId == vacation.Id); // but sells outside the window
    }

    [Fact]
    public void Generate_TerminatedManager_IsInactiveWithNoRecentSales()
    {
        var data = Generate();
        var terminated = data.Managers[11]; // fixed profile order: index 11 is the terminated profile
        var cutoff = Now.AddDays(-90).UtcDateTime;

        Assert.False(terminated.IsActive);
        Assert.DoesNotContain(data.Sales, s => s.ManagerId == terminated.Id && s.SoldAt >= cutoff);
        Assert.Contains(data.Sales, s => s.ManagerId == terminated.Id); // had sales before termination
    }

    [Fact]
    public void Generate_PricedItems_UseTheProductVersionActiveAtSoldAt()
    {
        var data = Generate();
        var updateInstant = Now.UtcDateTime.AddDays(-180);
        var soldAtBySale = data.Sales.ToDictionary(s => s.Id, s => s.SoldAt);

        // Find a product that was re-priced mid-year: its items carry two distinct version instants.
        var updatedProduct = data.SaleItems
            .GroupBy(i => i.ProductId)
            .FirstOrDefault(g => g.Select(i => i.ProductVersionAt).Distinct().Count() > 1);

        Assert.NotNull(updatedProduct);

        foreach (var item in updatedProduct)
        {
            var soldAt = soldAtBySale[item.SaleId];
            if (item.ProductVersionAt >= updateInstant)
            {
                Assert.True(soldAt >= updateInstant, "new-version items must be sold at/after the update");
            }
            else
            {
                Assert.True(soldAt < updateInstant, "old-version items must be sold before the update");
            }
        }
    }

    [Fact]
    public void Generate_SaleIds_AreUuidV7EncodingSoldAt()
    {
        var data = Generate();

        foreach (var sale in data.Sales)
        {
            Assert.Equal(7, VersionNibble(sale.Id));
            Assert.Equal(0b10, VariantBits(sale.Id));

            var expectedMs = new DateTimeOffset(sale.SoldAt, TimeSpan.Zero).ToUnixTimeMilliseconds();
            Assert.Equal(expectedMs, TimestampMs(sale.Id));
        }

        // Ordering by SoldAt is therefore ordering by the id timestamp (D13).
        var orderedBySoldAt = data.Sales.OrderBy(s => s.SoldAt).Select(s => TimestampMs(s.Id)).ToList();
        var sorted = orderedBySoldAt.Order().ToList();
        Assert.Equal(sorted, orderedBySoldAt);
    }

    [Fact]
    public void Generate_ReferencesAreConsistent()
    {
        var data = Generate();
        var managerIds = data.Managers.Select(m => m.Id).ToHashSet();
        var customerIds = data.Customers.Select(c => c.Id).ToHashSet();
        var productIds = data.Products.Select(p => p.Id).ToHashSet();
        var saleIds = data.Sales.Select(s => s.Id).ToHashSet();

        Assert.All(data.Sales, s => Assert.Contains(s.ManagerId, managerIds));
        Assert.All(data.Sales, s => Assert.Contains(s.CustomerId, customerIds));
        Assert.All(data.SaleItems, i => Assert.Contains(i.ProductId, productIds));
        Assert.All(data.SaleItems, i => Assert.Contains(i.SaleId, saleIds));
    }

    private static long TimestampMs(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes, bigEndian: true, out _);
        long ms = 0;
        for (var i = 0; i < 6; i++)
        {
            ms = (ms << 8) | bytes[i];
        }

        return ms;
    }

    private static int VersionNibble(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes, bigEndian: true, out _);
        return bytes[6] >> 4;
    }

    private static int VariantBits(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes, bigEndian: true, out _);
        return bytes[8] >> 6;
    }
}
