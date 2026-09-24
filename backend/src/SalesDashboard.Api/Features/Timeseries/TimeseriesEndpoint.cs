using Npgsql;
using Microsoft.Extensions.Options;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Timeseries;

/// <summary>
/// Revenue-over-time slice (D8, D9): <c>GET /api/dashboard/timeseries</c>. HTTP mapping only — parameters, the
/// call to <see cref="TimeseriesHandler"/>, the result-to-response mapping and OpenAPI metadata. The
/// application logic and SQL live in <see cref="TimeseriesHandler"/>.
/// </summary>
public static class TimeseriesEndpoint
{
    public static RouteGroupBuilder MapTimeseries(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/timeseries", HandleAsync)
            .WithName("GetTimeseries")
            .WithSummary("Revenue, gross profit and Paid sales count over time, bucketed by a server-chosen step.")
            .Produces<TimeseriesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] PeriodQuery query,
        NpgsqlDataSource dataSource,
        IOptions<ReportingOptions> reporting,
        CancellationToken cancellationToken)
    {
        var result = await TimeseriesHandler.HandleAsync(query, dataSource, reporting, cancellationToken)
            .ConfigureAwait(false);

        return result.IsError
            ? result.Errors.ToProblemResult()
            : Results.Ok(result.Value);
    }
}
