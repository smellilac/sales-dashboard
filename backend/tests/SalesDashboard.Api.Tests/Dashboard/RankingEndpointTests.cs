using System.Net;
using System.Net.Http.Json;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Features.Kpi;
using SalesDashboard.Api.Features.ManagerRanking;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>Integration tests for <c>GET /api/dashboard/managers/ranking</c> (T5 tests 8–13, plus best-manager 13).</summary>
public sealed class RankingEndpointTests(PostgresFixture postgres)
{
    private const string From = "2026-03-01";
    private const string To = "2026-03-31";

    private static readonly DateTime CurrentDay = DashboardTestHost.MoscowUtc(2026, 3, 15, 12);
    private static readonly DateTime PreviousDay = DashboardTestHost.MoscowUtc(2026, 2, 15, 12);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Ties_UseSportNumbering_ThenRevenueThenName()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            // Two tied at profit 400 (rank 1) then two tied at profit 300 (rank 3).
            var cole = await data.AddManagerAsync("Cara", "Cole");
            var adams = await data.AddManagerAsync("Ann", "Adams");
            var brown = await data.AddManagerAsync("Bob", "Brown");
            var dole = await data.AddManagerAsync("Dan", "Dole");

            await data.AddSaleAsync(cole, SaleStatus.Paid, CurrentDay, revenue: 1000m, cost: 600m);  // 400 / rev 1000
            await data.AddSaleAsync(adams, SaleStatus.Paid, CurrentDay, revenue: 900m, cost: 500m);  // 400 / rev 900
            await data.AddSaleAsync(brown, SaleStatus.Paid, CurrentDay, revenue: 900m, cost: 600m);  // 300 / rev 900
            await data.AddSaleAsync(dole, SaleStatus.Paid, CurrentDay, revenue: 900m, cost: 600m);   // 300 / rev 900
        });

        var ranking = await GetRankingAsync(app);

        Assert.Collection(
            ranking.Items,
            row => AssertRow(row, "Cole", rank: 1),   // rank 1, higher revenue
            row => AssertRow(row, "Adams", rank: 1),   // rank 1, lower revenue
            row => AssertRow(row, "Brown", rank: 3),   // rank 3, name before Dole
            row => AssertRow(row, "Dole", rank: 3));
    }

    [Fact]
    public async Task ManagerWithoutSales_IsLast_WithNullRank()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var seller = await data.AddManagerAsync("Ada", "Seller");
            await data.AddManagerAsync("Zoe", "Idle");
            await data.AddSaleAsync(seller, SaleStatus.Paid, CurrentDay, revenue: 1000m, cost: 600m);
        });

        var ranking = await GetRankingAsync(app);

        Assert.Collection(
            ranking.Items,
            row =>
            {
                Assert.Equal("Seller", row.LastName);
                Assert.Equal(1, row.Rank);
                Assert.True(row.HasSales);
            },
            row =>
            {
                Assert.Equal("Idle", row.LastName);
                Assert.Null(row.Rank);
                Assert.False(row.HasSales);
            });
    }

    [Fact]
    public async Task Inactive_HiddenWithoutSales_ShownWithSales()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var active = await data.AddManagerAsync("Ada", "Active");
            var firedSeller = await data.AddManagerAsync("Bob", "Fired", isActive: false);
            await data.AddManagerAsync("Cy", "Gone", isActive: false);

            await data.AddSaleAsync(active, SaleStatus.Paid, CurrentDay, revenue: 1000m, cost: 600m);
            await data.AddSaleAsync(firedSeller, SaleStatus.Paid, CurrentDay, revenue: 800m, cost: 500m);
        });

        var ranking = await GetRankingAsync(app);

        Assert.DoesNotContain(ranking.Items, row => string.Equals(row.LastName, "Gone", StringComparison.Ordinal));
        var fired = ranking.Items.Single(row => string.Equals(row.LastName, "Fired", StringComparison.Ordinal));
        Assert.False(fired.IsActive);
        Assert.True(fired.HasSales);
    }

    [Fact]
    public async Task RankBy_AverageCheck_ReordersRows_UnknownReturns400()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var big = await data.AddManagerAsync("Ada", "Big");
            var many = await data.AddManagerAsync("Bob", "Many");

            // Big: one large sale -> profit 900, avg 1000.
            await data.AddSaleAsync(big, SaleStatus.Paid, CurrentDay, revenue: 1000m, cost: 100m);
            // Many: three sales -> profit 1350, avg 500.
            await data.AddSaleAsync(many, SaleStatus.Paid, CurrentDay, revenue: 500m, cost: 50m);
            await data.AddSaleAsync(many, SaleStatus.Paid, CurrentDay, revenue: 500m, cost: 50m);
            await data.AddSaleAsync(many, SaleStatus.Paid, CurrentDay, revenue: 500m, cost: 50m);
        });

        var byProfit = await GetRankingAsync(app);
        Assert.Equal("grossProfit", byProfit.RankBy);
        Assert.Equal("Many", byProfit.Items[0].LastName);   // 1350 > 900
        Assert.Equal("Big", byProfit.Items[1].LastName);

        var byAverage = await GetRankingAsync(app, rankBy: "averageCheck");
        Assert.Equal("averageCheck", byAverage.RankBy);
        Assert.Equal("Big", byAverage.Items[0].LastName);   // avg 1000 > 500
        Assert.Equal("Many", byAverage.Items[1].LastName);

        using var unknown = await app.Client.GetAsync(
            new Uri($"/api/dashboard/managers/ranking?from={From}&to={To}&rankBy=nonsense", UriKind.Relative),
            Ct);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [Fact]
    public async Task MetricChange_IsNull_WhenPreviousPeriodIsZero()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var compared = await data.AddManagerAsync("Ada", "Compared");
            var fresh = await data.AddManagerAsync("Bob", "Fresh");

            // Compared: current profit 400, previous profit 200 -> change +1.0.
            await data.AddSaleAsync(compared, SaleStatus.Paid, CurrentDay, revenue: 1000m, cost: 600m);
            await data.AddSaleAsync(compared, SaleStatus.Paid, PreviousDay, revenue: 500m, cost: 300m);
            // Fresh: current only, no previous -> change null.
            await data.AddSaleAsync(fresh, SaleStatus.Paid, CurrentDay, revenue: 900m, cost: 600m);
        });

        var ranking = await GetRankingAsync(app);

        var compared = ranking.Items.Single(row => string.Equals(row.LastName, "Compared", StringComparison.Ordinal));
        Assert.Equal(1.0m, compared.MetricChange);

        var fresh = ranking.Items.Single(row => string.Equals(row.LastName, "Fresh", StringComparison.Ordinal));
        Assert.Null(fresh.MetricChange);
    }

    [Fact]
    public async Task BestManager_ReportsTiedCount_AtFirstPlace()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var first = await data.AddManagerAsync("Ada", "First");
            var alsoFirst = await data.AddManagerAsync("Bob", "Alsofirst");
            var third = await data.AddManagerAsync("Cy", "Third");

            await data.AddSaleAsync(first, SaleStatus.Paid, CurrentDay, revenue: 1000m, cost: 500m);       // 500
            await data.AddSaleAsync(alsoFirst, SaleStatus.Paid, CurrentDay, revenue: 900m, cost: 400m);    // 500
            await data.AddSaleAsync(third, SaleStatus.Paid, CurrentDay, revenue: 800m, cost: 500m);        // 300
        });

        var kpi = await app.Client.GetFromJsonAsync<KpiResponse>(
            new Uri($"/api/dashboard/kpis?from={From}&to={To}", UriKind.Relative),
            Ct);

        Assert.NotNull(kpi);
        Assert.NotNull(kpi.BestManager);
        Assert.Equal(500m, kpi.BestManager.GrossProfit);
        Assert.Equal(1, kpi.BestManager.TiedCount);
    }

    private static void AssertRow(RankingRow row, string lastName, int? rank)
    {
        Assert.Equal(lastName, row.LastName);
        Assert.Equal(rank, row.Rank);
    }

    private static async Task<RankingResponse> GetRankingAsync(DashboardApp app, string? rankBy = null)
    {
        var suffix = rankBy is null ? string.Empty : $"&rankBy={rankBy}";
        var ranking = await app.Client.GetFromJsonAsync<RankingResponse>(
            new Uri($"/api/dashboard/managers/ranking?from={From}&to={To}{suffix}", UriKind.Relative),
            Ct);
        Assert.NotNull(ranking);
        return ranking;
    }
}
