using Microsoft.Extensions.Options;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Kpi;

/// <summary>
/// KPI block slice (D8, D9): <c>GET /api/dashboard/kpis</c>. HTTP mapping only — parameters, the call to
/// <see cref="KpiHandler"/>, the result-to-response mapping and OpenAPI metadata. The application logic and
/// SQL live in <see cref="KpiHandler"/>.
/// </summary>
public static class KpiEndpoint
{
    public static RouteGroupBuilder MapKpis(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/kpis", HandleAsync)
            .WithName("GetKpis")
            .WithSummary("Dashboard KPI cards for the selected period versus the previous one.")
            .Produces<KpiResponse>(StatusCodes.Status200OK)
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
        var result = await KpiHandler.HandleAsync(query, db, reporting, cancellationToken).ConfigureAwait(false);

        return result.IsError
            ? result.Errors.ToProblemResult()
            : Results.Ok(result.Value);
    }
}
