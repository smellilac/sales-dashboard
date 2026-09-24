using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.TopProducts;

/// <summary>
/// Top products slice (D8, D9): <c>GET /api/dashboard/products/top</c>. HTTP mapping only — parameters, the
/// call to <see cref="TopProductsHandler"/>, the result-to-response mapping and OpenAPI metadata.
/// </summary>
public static class TopProductsEndpoint
{
    public static RouteGroupBuilder MapTopProducts(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/products/top", HandleAsync)
            .WithName("GetTopProducts")
            .WithSummary("The 10 products with the highest gross profit for the selected period.")
            .Produces<TopProductsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] PeriodQuery query,
        SalesDbContext db,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        var result = await TopProductsHandler.HandleAsync(query, db, reporting, cancellationToken)
            .ConfigureAwait(false);

        return result.IsError
            ? result.Errors.ToProblemResult()
            : Results.Ok(result.Value);
    }
}
