using System.Net;
using System.Net.Http.Json;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Features.RecentSales;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>Integration tests for <c>GET /api/dashboard/sales/recent</c> (T6 tests 7–9, RECENT-SALES).</summary>
public sealed class RecentSalesEndpointTests(PostgresFixture postgres)
{
    private const string From = "2026-03-01";
    private const string To = "2026-03-31";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Order_IsNewestFirst_AllStatusesWithSums()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 15, 12), 1000m, 600m);
            await data.AddSaleAsync(manager, SaleStatus.Cancelled, DashboardTestHost.MoscowUtc(2026, 3, 16, 12), 500m, 100m);
            await data.AddSaleAsync(manager, SaleStatus.Refunded, DashboardTestHost.MoscowUtc(2026, 3, 17, 12), 250m, 50m);
        });

        var page = await GetRecentAsync(app);

        Assert.Collection(
            page.Items,
            row =>
            {
                Assert.Equal(SaleStatus.Refunded, row.Status);   // newest (day 17)
                Assert.Equal(250m, row.Amount);
                Assert.Equal(200m, row.GrossProfit);
            },
            row =>
            {
                Assert.Equal(SaleStatus.Cancelled, row.Status);  // day 16 — sums still present (D2)
                Assert.Equal(500m, row.Amount);
                Assert.Equal(400m, row.GrossProfit);
            },
            row =>
            {
                Assert.Equal(SaleStatus.Paid, row.Status);       // day 15
                Assert.Equal(1000m, row.Amount);
                Assert.Equal(400m, row.GrossProfit);
            });

        var newest = page.Items[0];
        Assert.Equal("Ada Lovelace", newest.ManagerName);
        Assert.Equal("Contoso", newest.CustomerCompany);
        var item = Assert.Single(newest.Items);
        Assert.Equal("Mavic", item.ProductName);
        Assert.Equal(1, item.Quantity);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task Keyset_PagesWithoutDuplicatesOrGaps_AcrossEqualTimestampBoundary()
    {
        // Amounts identify sales. Two sales share day-19; they must split across a page boundary cleanly.
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            var day19 = DashboardTestHost.MoscowUtc(2026, 3, 19, 12);
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 20, 12), 100m, 0m); // s0
            await data.AddSaleAsync(manager, SaleStatus.Paid, day19, 200m, 0m);                                        // s1
            await data.AddSaleAsync(manager, SaleStatus.Paid, day19, 300m, 0m);                                        // s2 (later id)
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 18, 12), 400m, 0m); // s3
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 17, 12), 500m, 0m); // s4
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 16, 12), 600m, 0m); // s5
        });

        var amounts = new List<decimal>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = await GetRecentAsync(app, limit: 2, cursor: cursor);
            Assert.True(page.Items.Count <= 2);
            amounts.AddRange(page.Items.Select(r => r.Amount));
            cursor = page.NextCursor;
            pages++;
        }
        while (cursor is not null);

        Assert.Equal(3, pages);
        // sold_at desc, then id desc: same-time s2 (300) precedes s1 (200).
        Assert.Equal(new[] { 100m, 300m, 200m, 400m, 500m, 600m }, amounts);
        Assert.Equal(6, amounts.Distinct().Count());
    }

    [Fact]
    public async Task InvalidCursorOrLimit_Returns400()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, _ => Task.CompletedTask);

        Assert.Equal(HttpStatusCode.BadRequest, await GetStatusAsync(app, "&cursor=not-a-cursor"));
        Assert.Equal(HttpStatusCode.BadRequest, await GetStatusAsync(app, "&limit=0"));
        Assert.Equal(HttpStatusCode.BadRequest, await GetStatusAsync(app, "&limit=101"));
    }

    private static async Task<HttpStatusCode> GetStatusAsync(DashboardApp app, string suffix)
    {
        using var response = await app.Client.GetAsync(
            new Uri($"/api/dashboard/sales/recent?from={From}&to={To}{suffix}", UriKind.Relative),
            Ct);
        return response.StatusCode;
    }

    private static async Task<RecentSalesResponse> GetRecentAsync(DashboardApp app, int? limit = null, string? cursor = null)
    {
        var suffix = limit is null ? string.Empty : $"&limit={limit}";
        if (cursor is not null)
        {
            suffix += $"&cursor={Uri.EscapeDataString(cursor)}";
        }

        var page = await app.Client.GetFromJsonAsync<RecentSalesResponse>(
            new Uri($"/api/dashboard/sales/recent?from={From}&to={To}{suffix}", UriKind.Relative),
            Ct);
        Assert.NotNull(page);
        return page;
    }
}
