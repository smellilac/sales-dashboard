using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.ManagerRanking;

/// <summary>
/// Manager ranking slice (D8, D9): <c>GET /api/dashboard/managers/ranking</c>. HTTP mapping only — parameters,
/// the call to <see cref="RankingHandler"/>, the result-to-response mapping and OpenAPI metadata. The
/// application logic and SQL live in <see cref="RankingHandler"/>.
/// </summary>
public static class RankingEndpoint
{
    public static RouteGroupBuilder MapManagerRanking(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/managers/ranking", HandleAsync)
            .WithName("GetManagerRanking")
            .WithSummary("Managers ranked by gross profit or average check for the selected period.")
            .Produces<RankingResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] PeriodQuery query,
        [FromQuery] string? rankBy,
        SalesDbContext db,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        var result = await RankingHandler.HandleAsync(query, rankBy, db, reporting, cancellationToken)
            .ConfigureAwait(false);

        return result.IsError
            ? result.Errors.ToProblemResult()
            : Results.Ok(result.Value);
    }
}
