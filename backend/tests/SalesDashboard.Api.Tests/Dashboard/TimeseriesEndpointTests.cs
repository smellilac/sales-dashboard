using System.Net.Http.Json;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Features.Timeseries;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>Integration tests for <c>GET /api/dashboard/timeseries</c> against real PostgreSQL (T6 tests 1–4).</summary>
public sealed class TimeseriesEndpointTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("2026-03-01", "2026-03-31", "day")]    // 31 days
    [InlineData("2026-03-01", "2026-04-01", "week")]   // 32 days
    [InlineData("2026-03-01", "2026-08-27", "week")]   // 180 days
    [InlineData("2026-03-01", "2026-08-28", "month")]  // 181 days
    public async Task Granularity_ChosenByPeriodLength(string from, string to, string expected)
    {
        await using var app = await DashboardTestHost.StartAsync(postgres, _ => Task.CompletedTask);

        var series = await GetTimeseriesAsync(app, from, to);

        Assert.Equal(expected, series.Granularity);
    }

    [Fact]
    public async Task Weekly_EmptyBucketsAreZero_WeeksStartMonday_EdgesClipped()
    {
        // from is a Wednesday; the week it falls in starts Monday 2026-03-02.
        const string From = "2026-03-04";
        const string To = "2026-05-15";

        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            // Tuesday 2026-03-10 -> the second week bucket (2026-03-09..2026-03-15).
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 10, 12), 700m, 200m);
        });

        var series = await GetTimeseriesAsync(app, From, To);

        Assert.Equal("week", series.Granularity);

        // Edge buckets carry the clipped period dates.
        Assert.Equal(new DateOnly(2026, 3, 4), series.Points[0].BucketStart);
        Assert.Equal(new DateOnly(2026, 5, 15), series.Points[^1].BucketEnd);

        // Interior buckets are full Monday..Sunday weeks.
        foreach (var point in series.Points.Skip(1))
        {
            Assert.Equal(DayOfWeek.Monday, point.BucketStart.DayOfWeek);
        }

        foreach (var point in series.Points.Take(series.Points.Count - 1))
        {
            Assert.Equal(DayOfWeek.Sunday, point.BucketEnd.DayOfWeek);
        }

        // The sale lands in exactly one bucket; every other bucket is zero.
        var withSales = series.Points.Where(p => p.SalesCount > 0).ToList();
        var hit = Assert.Single(withSales);
        Assert.Equal(new DateOnly(2026, 3, 9), hit.BucketStart);
        Assert.Equal(new DateOnly(2026, 3, 15), hit.BucketEnd);
        Assert.Equal(700m, hit.Revenue);
        Assert.Equal(500m, hit.GrossProfit);

        Assert.Equal(700m, series.Points.Sum(p => p.Revenue));
        Assert.Equal(1, series.Points.Sum(p => p.SalesCount));
    }

    [Fact]
    public async Task SaleAfterMidnightMoscow_FallsInItsOwnDay_NotPreviousUtcDay()
    {
        const string From = "2026-03-01";
        const string To = "2026-03-05";

        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            // 01:30 Moscow on 2026-03-02 is 22:30 UTC on 2026-03-01; it must count on the 2nd (D4).
            await data.AddSaleAsync(manager, SaleStatus.Paid, DashboardTestHost.MoscowUtc(2026, 3, 2, 1, 30), 100m, 40m);
        });

        var series = await GetTimeseriesAsync(app, From, To);

        Assert.Equal("day", series.Granularity);

        var second = series.Points.Single(p => p.BucketStart == new DateOnly(2026, 3, 2));
        Assert.Equal(1, second.SalesCount);
        Assert.Equal(100m, second.Revenue);

        var first = series.Points.Single(p => p.BucketStart == new DateOnly(2026, 3, 1));
        Assert.Equal(0, first.SalesCount);
        Assert.Equal(0m, first.Revenue);
    }

    [Fact]
    public async Task OnlyPaidSales_Count()
    {
        const string From = "2026-03-01";
        const string To = "2026-03-31";
        var day = DashboardTestHost.MoscowUtc(2026, 3, 15, 12);

        await using var app = await DashboardTestHost.StartAsync(postgres, async data =>
        {
            var manager = await data.AddManagerAsync("Ada", "Lovelace");
            await data.AddSaleAsync(manager, SaleStatus.Paid, day, 1000m, 600m);
            await data.AddSaleAsync(manager, SaleStatus.Cancelled, day, 500m, 100m);
            await data.AddSaleAsync(manager, SaleStatus.Refunded, day, 250m, 50m);
        });

        var series = await GetTimeseriesAsync(app, From, To);

        Assert.Equal(1000m, series.Points.Sum(p => p.Revenue));
        Assert.Equal(400m, series.Points.Sum(p => p.GrossProfit));
        Assert.Equal(1, series.Points.Sum(p => p.SalesCount));
    }

    private static async Task<TimeseriesResponse> GetTimeseriesAsync(DashboardApp app, string from, string to)
    {
        var series = await app.Client.GetFromJsonAsync<TimeseriesResponse>(
            new Uri($"/api/dashboard/timeseries?from={from}&to={to}", UriKind.Relative),
            Ct);
        Assert.NotNull(series);
        return series;
    }
}
