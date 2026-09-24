using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.RecentSales;

/// <summary>
/// Application logic for the recent-sales feed (D8, D9). One EF Core query (D10) per page: keyset pagination on
/// <c>(sold_at desc, id desc)</c> via a row-value comparison (RECENT-SALES), all statuses included with each
/// sale's line totals (D2), and the lines projected inline so there is no N+1. Returns an
/// <see cref="ErrorOr{T}"/> so the endpoint owns the HTTP mapping.
/// </summary>
public static class RecentSalesHandler
{
    public static async Task<ErrorOr<RecentSalesResponse>> HandleAsync(
        PeriodQuery query,
        int? limit,
        string? cursor,
        SalesDbContext db,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(reporting);

        var limitResult = ResolveLimit(limit);
        if (limitResult.IsError)
        {
            return limitResult.Errors;
        }

        var cursorResult = ParseCursor(cursor);
        if (cursorResult.IsError)
        {
            return cursorResult.Errors;
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(reporting.Value.TimeZone);

        var periodResult = ReportingPeriod.Create(query.From, query.To, zone);
        if (periodResult.IsError)
        {
            return periodResult.Errors;
        }

        var period = periodResult.Value.Current;
        var pageSize = limitResult.Value;

        // Fetch one extra row to know whether a further page exists.
        var rows = await FetchPageAsync(db, period, cursorResult.Value, pageSize + 1, cancellationToken)
            .ConfigureAwait(false);

        string? nextCursor = null;
        if (rows.Count > pageSize)
        {
            rows.RemoveAt(rows.Count - 1);
            var last = rows[^1];
            nextCursor = new RecentSalesCursor(last.SoldAt, last.SaleId).Encode();
        }

        return new RecentSalesResponse(rows, nextCursor);
    }

    private static async Task<List<RecentSaleRow>> FetchPageAsync(
        SalesDbContext db,
        DateRange period,
        RecentSalesCursor? keyset,
        int take,
        CancellationToken cancellationToken)
    {
        var sales = db.Sales.AsNoTracking()
            .Where(s => s.SoldAt >= period.StartUtc && s.SoldAt < period.EndUtc);

        if (keyset is { } after)
        {
            // Row-value comparison: (sold_at, id) < (cursor.sold_at, cursor.id) walks the desc order (RECENT-SALES).
            sales = sales.Where(s =>
                EF.Functions.LessThan(
                    ValueTuple.Create(s.SoldAt, s.Id),
                    ValueTuple.Create(after.SoldAt, after.Id)));
        }

        return await sales
            .OrderByDescending(s => s.SoldAt)
            .ThenByDescending(s => s.Id)
            .Take(take)
            .Select(s => new RecentSaleRow(
                s.Id,
                s.SoldAt,
                s.ManagerId,
                s.Manager!.FirstName + " " + s.Manager.LastName,
                s.Customer!.Company,
                s.Status,
                s.Items.Sum(i => i.LineRevenue),
                s.Items.Sum(i => i.LineRevenue - i.LineCost),
                s.Items
                    .OrderBy(i => i.Product!.Name)
                    .Select(i => new RecentSaleItemDto(i.Product!.Name, i.Quantity))
                    .ToList()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static ErrorOr<int> ResolveLimit(int? limit)
    {
        if (limit is null)
        {
            return RecentSalesErrorCodes.DefaultLimit;
        }

        if (limit < RecentSalesErrorCodes.MinLimit || limit > RecentSalesErrorCodes.MaxLimit)
        {
            return Error.Validation(
                code: RecentSalesErrorCodes.InvalidLimit,
                description: $"'limit' must be between {RecentSalesErrorCodes.MinLimit} and {RecentSalesErrorCodes.MaxLimit}.",
                metadata: Field(RecentSalesErrorCodes.LimitField));
        }

        return limit.Value;
    }

    private static ErrorOr<RecentSalesCursor?> ParseCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            return default(RecentSalesCursor?);
        }

        if (!RecentSalesCursor.TryDecode(cursor, out var parsed))
        {
            return Error.Validation(
                code: RecentSalesErrorCodes.InvalidCursor,
                description: "'cursor' is not a valid pagination cursor.",
                metadata: Field(RecentSalesErrorCodes.CursorField));
        }

        return parsed;
    }

    private static Dictionary<string, object> Field(string field) =>
        new(StringComparer.Ordinal) { [PeriodErrorCodes.FieldKey] = field };
}
