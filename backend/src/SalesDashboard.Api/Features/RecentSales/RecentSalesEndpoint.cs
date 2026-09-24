using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.RecentSales;

/// <summary>
/// Recent-sales slice (D8, D9): <c>GET /api/dashboard/sales/recent</c>. HTTP mapping only — parameters, the
/// call to <see cref="RecentSalesHandler"/>, the result-to-response mapping and OpenAPI metadata.
/// </summary>
public static class RecentSalesEndpoint
{
    public static RouteGroupBuilder MapRecentSales(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/sales/recent", HandleAsync)
            .WithName("GetRecentSales")
            .WithSummary("Keyset-paginated feed of recent sales (all statuses) for the selected period.")
            .Produces<RecentSalesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] PeriodQuery query,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        SalesDbContext db,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        var result = await RecentSalesHandler.HandleAsync(query, limit, cursor, db, reporting, cancellationToken)
            .ConfigureAwait(false);

        return result.IsError
            ? result.Errors.ToProblemResult()
            : Results.Ok(result.Value);
    }
}
