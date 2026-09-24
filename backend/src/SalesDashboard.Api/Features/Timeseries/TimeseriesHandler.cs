using Dapper;
using ErrorOr;
using Microsoft.Extensions.Options;
using Npgsql;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Timeseries;

/// <summary>
/// Application logic for the revenue-over-time block (D8, D9). One Dapper query (D10): the aggregation and the
/// empty-bucket fill both belong in SQL, and <c>date_trunc</c> in the business zone is awkward to express in
/// LINQ. Bucketing runs on <c>sold_at AT TIME ZONE @tz</c> (D4) while the period filter stays on raw UTC
/// bounds so it can use the index; only Paid sales count, and <c>salesCount</c> is per sale, not per line (D2).
/// Returns an <see cref="ErrorOr{T}"/> so the endpoint owns the HTTP mapping.
/// </summary>
public static class TimeseriesHandler
{
    // Aggregates lines per sale first, so COUNT(*) counts Paid sales (not line items, D2). Buckets by the
    // business-zone day (D4); the WHERE stays on raw sold_at UTC bounds. generate_series fills empty buckets,
    // and GREATEST/LEAST clip the edge buckets to the requested days (TIMESERIES). Column aliases are quoted
    // to match the row type without a global underscore map. @step is also the date_trunc unit.
    private const string Sql =
        """
        WITH agg AS (
            SELECT date_trunc(@step, s.sold_at AT TIME ZONE @tz) AS bucket_start,
                   SUM(m.revenue) AS revenue,
                   SUM(m.cost)    AS cost,
                   COUNT(*)::int  AS sales_count
            FROM sales s
            JOIN (
                SELECT sale_id, SUM(line_revenue) AS revenue, SUM(line_cost) AS cost
                FROM sale_items
                GROUP BY sale_id
            ) m ON m.sale_id = s.id
            WHERE s.status = 'Paid' AND s.sold_at >= @startUtc AND s.sold_at < @endUtc
            GROUP BY 1
        ),
        series AS (
            SELECT gs AS bucket_start
            FROM generate_series(
                date_trunc(@step, @fromDay::timestamp),
                date_trunc(@step, @toDay::timestamp),
                CASE @step WHEN 'day'  THEN interval '1 day'
                           WHEN 'week' THEN interval '7 days'
                           ELSE interval '1 month' END) AS gs
        )
        SELECT
            GREATEST(series.bucket_start::date, @fromDay::date)                AS "bucketStart",
            LEAST((series.bucket_start
                   + CASE @step WHEN 'day'  THEN interval '1 day'
                                WHEN 'week' THEN interval '7 days'
                                ELSE interval '1 month' END
                   - interval '1 day')::date, @toDay::date)                    AS "bucketEnd",
            COALESCE(agg.revenue, 0)                                           AS "revenue",
            COALESCE(agg.revenue, 0) - COALESCE(agg.cost, 0)                   AS "grossProfit",
            COALESCE(agg.sales_count, 0)                                       AS "salesCount"
        FROM series
        LEFT JOIN agg ON agg.bucket_start = series.bucket_start
        ORDER BY series.bucket_start;
        """;

    public static async Task<ErrorOr<TimeseriesResponse>> HandleAsync(
        PeriodQuery query,
        NpgsqlDataSource dataSource,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(reporting);

        var zoneId = reporting.Value.TimeZone;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);

        var periodResult = ReportingPeriod.Create(query.From, query.To, zone);
        if (periodResult.IsError)
        {
            return periodResult.Errors;
        }

        var period = periodResult.Value.Current;
        var granularity = TimeseriesGranularitySelector.ForPeriod(period);

        var parameters = new
        {
            step = granularity.ToTruncField(),
            tz = zoneId,
            startUtc = period.StartUtc,
            endUtc = period.EndUtc,
            fromDay = period.From,
            toDay = period.To,
        };

        var command = new CommandDefinition(Sql, parameters, cancellationToken: cancellationToken);

        var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var rows = await connection.QueryAsync<TimeseriesRow>(command).ConfigureAwait(false);

            var points = rows
                .Select(r => new TimeseriesPoint(r.BucketStart, r.BucketEnd, r.Revenue, r.GrossProfit, r.SalesCount))
                .ToList();

            return new TimeseriesResponse(
                Granularity: granularity.ToWire(),
                Period: new PeriodDto(period.From, period.To),
                Points: points);
        }
    }

    /// <summary>Dapper projection of one chart bucket; column aliases in <see cref="Sql"/> match these names.</summary>
    private sealed record TimeseriesRow(
        DateOnly BucketStart,
        DateOnly BucketEnd,
        decimal Revenue,
        decimal GrossProfit,
        int SalesCount);
}
