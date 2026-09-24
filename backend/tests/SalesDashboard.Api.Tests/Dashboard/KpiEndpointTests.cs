using System.Net;
using System.Net.Http.Json;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Features.Kpi;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>Integration tests for <c>GET /api/dashboard/kpis</c> against real PostgreSQL (T5 tests 1–7).</summary>
public sealed class KpiEndpointTests(PostgresFixture postgres)
{
    // A 31-day window. Previous window (same length, immediately before) is 2026-01-29..2026-02-28 (D3).
    private const string From = "2026-03-01";
    private const string To = "2026-03-31";

    private static readonly DateTime CurrentDay = DashboardTestHost.MoscowUtc(2026, 3, 15, 12);
    private static readonly DateTime PreviousDay = DashboardTestHost.MoscowUtc(2026, 2, 15, 12);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Totals_CountOnlyPaidSales()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            await data.AddSaleAsync(manager, SaleStatus.Paid, CurrentDay, revenue: 1000m, cost: 600m);
            await data.AddSaleAsync(manager, SaleStatus.Paid, CurrentDay, revenue: 500m, cost: 300m);
        });

        var kpi = await GetKpisAsync(app);

        Assert.Equal(1500m, kpi.Revenue.Current);
        Assert.Equal(600m, kpi.GrossProfit.Current);
        Assert.Equal(2, kpi.SalesCount.Current);
        Assert.Equal(750m, kpi.AverageCheck.Current);
        Assert.Equal(0.4m, kpi.Margin.Current);
    }

    [Fact]
    public async Task CancelledAndRefunded_ExcludedFromTotals_AndRefundsReported()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            await data.AddSaleAsync(manager, SaleStatus.Paid, CurrentDay, revenue: 1000m, cost: 600m);
            await data.AddSaleAsync(manager, SaleStatus.Cancelled, CurrentDay, revenue: 500m, cost: 100m);
            await data.AddSaleAsync(manager, SaleStatus.Refunded, CurrentDay, revenue: 250m, cost: 50m);
        });

        var kpi = await GetKpisAsync(app);

        Assert.Equal(1000m, kpi.Revenue.Current);
        Assert.Equal(400m, kpi.GrossProfit.Current);
        Assert.Equal(1, kpi.SalesCount.Current);

        Assert.Equal(250m, kpi.Refunds.Amount.Current);
        // rate = refunded / (paid + refunded) = 250 / 1250 = 0.2 (D1).
        Assert.Equal(0.2m, kpi.Refunds.Rate.Current);
    }

    [Fact]
    public async Task PreviousPeriod_DrivesChangeAndChangePoints()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            // Current: revenue 1200, cost 720 -> margin 0.4.
            await data.AddSaleAsync(manager, SaleStatus.Paid, CurrentDay, revenue: 1200m, cost: 720m);
            // Previous: revenue 1000, cost 700 -> margin 0.3.
            await data.AddSaleAsync(manager, SaleStatus.Paid, PreviousDay, revenue: 1000m, cost: 700m);
        });

        var kpi = await GetKpisAsync(app);

        Assert.Equal(1000m, kpi.Revenue.Previous);
        Assert.Equal(0.2m, kpi.Revenue.Change);          // (1200-1000)/1000
        Assert.Equal(0.2m, kpi.AverageCheck.Change);     // 1 sale each side
        Assert.Equal(0m, kpi.SalesCount.Change);
        Assert.Equal(0.1m, kpi.Margin.ChangePoints);     // 0.4 - 0.3

        Assert.Equal(new DateOnly(2026, 1, 29), kpi.PreviousPeriod.From);
        Assert.Equal(new DateOnly(2026, 2, 28), kpi.PreviousPeriod.To);
    }

    [Fact]
    public async Task EmptyPeriod_ZeroSums_NullRatiosAndChanges_NullBestManager()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            // An active manager with no sales at all.
            await data.AddManagerAsync("Ada", "Lovelace");
        });

        var kpi = await GetKpisAsync(app);

        Assert.Equal(0m, kpi.Revenue.Current);
        Assert.Equal(0m, kpi.Revenue.Previous);
        Assert.Null(kpi.Revenue.Change);
        Assert.Equal(0, kpi.SalesCount.Current);
        Assert.Null(kpi.SalesCount.Change);
        Assert.Null(kpi.AverageCheck.Current);
        Assert.Null(kpi.AverageCheck.Change);
        Assert.Null(kpi.Margin.Current);
        Assert.Null(kpi.Margin.ChangePoints);
        Assert.Equal(0m, kpi.Refunds.Amount.Current);
        Assert.Null(kpi.Refunds.Rate.Current);
        Assert.Null(kpi.BestManager);
    }

    [Fact]
    public async Task DayBoundaries_AreHalfOpenInMoscow()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            // from 00:00 -> in; to 23:59:59.999 -> in; to+1 00:00 -> out (D4).
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 1, 0, 0, 0), 100m, 0m);
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 31, 23, 59, 59, 999), 100m, 0m);
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 4, 1, 0, 0, 0), 100m, 0m);
        });

        var kpi = await GetKpisAsync(app);

        Assert.Equal(2, kpi.SalesCount.Current);
        Assert.Equal(200m, kpi.Revenue.Current);
    }

    [Fact]
    public async Task LargeSaleAmongSmall_SumsWithoutPrecisionLoss()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            await data.AddSaleAsync(manager, SaleStatus.Paid, CurrentDay, revenue: 12_345_678.90m, cost: 0.10m);
            await data.AddSaleAsync(manager, SaleStatus.Paid, CurrentDay, revenue: 0.05m, cost: 0.02m);
        });

        var kpi = await GetKpisAsync(app);

        Assert.Equal(12_345_678.95m, kpi.Revenue.Current);
        Assert.Equal(12_345_678.83m, kpi.GrossProfit.Current);
    }

    [Fact]
    public async Task FromAfterTo_Returns400()
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, _ => Task.CompletedTask);

        using var response = await app.Client.GetAsync(
            new Uri("/api/dashboard/kpis?from=2026-03-31&to=2026-03-01", UriKind.Relative),
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<KpiResponse> GetKpisAsync(DashboardApp app)
    {
        var kpi = await app.Client.GetFromJsonAsync<KpiResponse>(
            new Uri($"/api/dashboard/kpis?from={From}&to={To}", UriKind.Relative),
            Ct);
        Assert.NotNull(kpi);
        return kpi;
    }
}
