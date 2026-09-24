using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Categories;

/// <summary>
/// Category breakdown slice (D8, D9): <c>GET /api/dashboard/categories</c>. HTTP mapping only — parameters, the
/// call to <see cref="CategoriesHandler"/>, the result-to-response mapping and OpenAPI metadata.
/// </summary>
public static class CategoriesEndpoint
{
    public static RouteGroupBuilder MapCategories(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/categories", HandleAsync)
            .WithName("GetCategories")
            .WithSummary("Revenue, profit, margin, units and revenue share per product category.")
            .Produces<CategoriesResponse>(StatusCodes.Status200OK)
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
        var result = await CategoriesHandler.HandleAsync(query, db, reporting, cancellationToken)
            .ConfigureAwait(false);

        return result.IsError
            ? result.Errors.ToProblemResult()
            : Results.Ok(result.Value);
    }
}
