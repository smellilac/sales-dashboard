using System.Globalization;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Data.Seed;
using SalesDashboard.Api.Features.Categories;
using SalesDashboard.Api.Features.Kpi;
using SalesDashboard.Api.Features.ManagerRanking;
using SalesDashboard.Api.Features.Timeseries;
using Xunit;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>
/// CONSISTENCY-TEST: over generated seed data, the revenue and gross profit summed across the timeseries, the
/// categories and the ranking must all equal the KPI totals, and the timeseries Paid-sale count must equal the
/// KPI count — for several period lengths, compared exactly as <see cref="decimal"/> (T6 test 10).
/// </summary>
public sealed class ConsistencyTests(PostgresFixture postgres)
{
    // Same fixed instant as the seed generator tests, so the data (and its window coverage) is reproducible.
    private static readonly TimeZoneInfo Moscow = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.FromHours(3));

    // A day fully inside the seeded 12 months (the day before "now" in the business zone).
    private static readonly DateOnly AnchorTo = new(2026, 9, 23);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(365)]
    public async Task Blocks_AgreeWithKpi_ForPeriod(int lengthDays)
    {
        await using var app = await DashboardTestHost.StartRawAsync(postgres, async db =>
        {
            var data = SeedDataGenerator.Generate(Now, Moscow);
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.Managers.AddRange(data.Managers);
            db.Customers.AddRange(data.Customers);
            db.Categories.AddRange(data.Categories);
            db.Products.AddRange(data.Products);
            db.Sales.AddRange(data.Sales);
            db.SaleItems.AddRange(data.SaleItems);
            await db.SaveChangesAsync(Ct);
        });

        var from = AnchorTo.AddDays(-(lengthDays - 1));
        var to = AnchorTo;

        var kpi = await GetAsync<KpiResponse>(app, "kpis", from, to);
        var series = await GetAsync<TimeseriesResponse>(app, "timeseries", from, to);
        var categories = await GetAsync<CategoriesResponse>(app, "categories", from, to);
        var ranking = await GetAsync<RankingResponse>(app, "managers/ranking", from, to);

        var kpiRevenue = kpi.Revenue.Current;
        var kpiProfit = kpi.GrossProfit.Current;
        var kpiCount = kpi.SalesCount.Current;

        Assert.Equal(kpiRevenue, series.Points.Sum(p => p.Revenue));
        Assert.Equal(kpiRevenue, categories.Items.Sum(c => c.Revenue));
        Assert.Equal(kpiRevenue, ranking.Items.Sum(r => r.Revenue));

        Assert.Equal(kpiProfit, series.Points.Sum(p => p.GrossProfit));
        Assert.Equal(kpiProfit, categories.Items.Sum(c => c.GrossProfit));
        Assert.Equal(kpiProfit, ranking.Items.Sum(r => r.GrossProfit));

        Assert.Equal(kpiCount, series.Points.Sum(p => p.SalesCount));
    }

    private static async Task<T> GetAsync<T>(DashboardApp app, string block, DateOnly from, DateOnly to)
    {
        var fromText = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toText = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var value = await app.Client.GetFromJsonAsync<T>(
            new Uri($"/api/dashboard/{block}?from={fromText}&to={toText}", UriKind.Relative),
            Ct);
        Assert.NotNull(value);
        return value;
    }
}
