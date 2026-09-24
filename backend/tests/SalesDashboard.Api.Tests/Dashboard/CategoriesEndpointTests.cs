using System.Net.Http.Json;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Features.Categories;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>Integration tests for <c>GET /api/dashboard/categories</c> (T6 test 5, CAT-COUNT).</summary>
public sealed class CategoriesEndpointTests(PostgresFixture postgres)
{
    private const string From = "2026-03-01";
    private const string To = "2026-03-31";

    private static readonly DateTime CurrentDay = DashboardTestHost.MoscowUtc(2026, 3, 15, 12);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CategoryWithoutSales_AppearsWithZeros_ShareAndMarginPerCategory()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");

            var drones = await data.AddCategoryAsync("Drones");
            var mavic = await data.AddProductAsync(drones, "Mavic");
            await data.AddCategoryAsync("Batteries"); // no products, no sales

            // revenue 1000, cost 600, units 2.
            await data.AddSaleForProductAsync(manager, mavic, SaleStatus.Paid, CurrentDay, quantity: 2, unitPrice: 500m, unitCost: 300m);
        });

        var categories = await GetCategoriesAsync(app);

        Assert.Collection(
            categories.Items,
            drones =>
            {
                Assert.Equal("Drones", drones.Name);
                Assert.Equal(1000m, drones.Revenue);
                Assert.Equal(400m, drones.GrossProfit);
                Assert.Equal(0.4m, drones.Margin);
                Assert.Equal(2, drones.UnitsSold);
                Assert.Equal(1.0m, drones.RevenueShare); // 1000 / 1000
            },
            batteries =>
            {
                Assert.Equal("Batteries", batteries.Name);
                Assert.Equal(0m, batteries.Revenue);
                Assert.Equal(0m, batteries.GrossProfit);
                Assert.Null(batteries.Margin);          // no revenue (D6)
                Assert.Equal(0, batteries.UnitsSold);
                Assert.Equal(0m, batteries.RevenueShare); // 0 / 1000
            });
    }

    [Fact]
    public async Task RevenueShare_IsNull_WhenTotalRevenueIsZero()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            var drones = await data.AddCategoryAsync("Drones");
            var mavic = await data.AddProductAsync(drones, "Mavic");

            // Only a cancelled sale -> no Paid revenue anywhere (D2).
            await data.AddSaleForProductAsync(manager, mavic, SaleStatus.Cancelled, CurrentDay, quantity: 1, unitPrice: 500m, unitCost: 300m);
        });

        var categories = await GetCategoriesAsync(app);

        var drones = Assert.Single(categories.Items);
        Assert.Equal(0m, drones.Revenue);
        Assert.Null(drones.Margin);
        Assert.Null(drones.RevenueShare); // total revenue 0 (D6)
    }

    private static async Task<CategoriesResponse> GetCategoriesAsync(DashboardApp app)
    {
        var categories = await app.Client.GetFromJsonAsync<CategoriesResponse>(
            new Uri($"/api/dashboard/categories?from={From}&to={To}", UriKind.Relative),
            Ct);
        Assert.NotNull(categories);
        return categories;
    }
}
