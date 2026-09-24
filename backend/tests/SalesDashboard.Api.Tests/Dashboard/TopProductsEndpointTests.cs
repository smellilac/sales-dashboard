using System.Globalization;
using System.Net.Http.Json;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Features.TopProducts;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>Integration tests for <c>GET /api/dashboard/products/top</c> (T6 test 6, TOP-PRODUCTS).</summary>
public sealed class TopProductsEndpointTests(PostgresFixture postgres)
{
    private const string From = "2026-03-01";
    private const string To = "2026-03-31";

    private static readonly DateTime CurrentDay = DashboardTestHost.MoscowUtc(2026, 3, 15, 12);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Top_IsCappedAtTen_ExcludesUnsoldAndNonPaid()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            var category = await data.AddCategoryAsync("Drones");

            // 12 sold products with distinct, descending gross profit (1200, 1100, ... 100).
            for (var i = 1; i <= 12; i++)
            {
                var name = "P" + i.ToString("00", CultureInfo.InvariantCulture);
                var product = await data.AddProductAsync(category, name);
                var profit = (13 - i) * 100m; // i=1 -> 1200, i=12 -> 100
                await data.AddSaleForProductAsync(manager, product, SaleStatus.Paid, CurrentDay, quantity: 1, unitPrice: 2000m, unitCost: 2000m - profit);
            }

            // A product sold only as Cancelled must never appear.
            var ghost = await data.AddProductAsync(category, "Ghost");
            await data.AddSaleForProductAsync(manager, ghost, SaleStatus.Cancelled, CurrentDay, quantity: 1, unitPrice: 9999m, unitCost: 0m);
        });

        var top = await GetTopProductsAsync(app);

        Assert.Equal(10, top.Items.Count);
        Assert.Equal("P01", top.Items[0].Name);           // highest profit 1200
        Assert.Equal(1200m, top.Items[0].GrossProfit);
        Assert.Equal("P10", top.Items[^1].Name);          // 10th highest profit 300
        Assert.DoesNotContain(top.Items, p => string.Equals(p.Name, "Ghost", StringComparison.Ordinal));
        Assert.DoesNotContain(top.Items, p => string.Equals(p.Name, "P11", StringComparison.Ordinal));
        Assert.DoesNotContain(top.Items, p => string.Equals(p.Name, "P12", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Ties_BreakByRevenueThenName()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            var category = await data.AddCategoryAsync("Drones");

            // All three tie on gross profit 400. Charlie has the higher revenue; Alpha/Bravo tie on revenue too.
            var charlie = await data.AddProductAsync(category, "Charlie");
            var bravo = await data.AddProductAsync(category, "Bravo");
            var alpha = await data.AddProductAsync(category, "Alpha");

            await data.AddSaleForProductAsync(manager, charlie, SaleStatus.Paid, CurrentDay, quantity: 1, unitPrice: 1000m, unitCost: 600m); // rev 1000
            await data.AddSaleForProductAsync(manager, bravo, SaleStatus.Paid, CurrentDay, quantity: 1, unitPrice: 900m, unitCost: 500m);   // rev 900
            await data.AddSaleForProductAsync(manager, alpha, SaleStatus.Paid, CurrentDay, quantity: 1, unitPrice: 900m, unitCost: 500m);   // rev 900
        });

        var top = await GetTopProductsAsync(app);

        Assert.Collection(
            top.Items,
            p => Assert.Equal("Charlie", p.Name), // profit 400, revenue 1000 (highest)
            p => Assert.Equal("Alpha", p.Name),   // profit 400, revenue 900, name before Bravo
            p => Assert.Equal("Bravo", p.Name));
    }

    private static async Task<TopProductsResponse> GetTopProductsAsync(DashboardApp app)
    {
        var top = await app.Client.GetFromJsonAsync<TopProductsResponse>(
            new Uri($"/api/dashboard/products/top?from={From}&to={To}", UriKind.Relative),
            Ct);
        Assert.NotNull(top);
        return top;
    }
}
