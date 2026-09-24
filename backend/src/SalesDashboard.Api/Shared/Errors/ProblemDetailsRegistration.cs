using System.Diagnostics;

namespace SalesDashboard.Api.Shared.Errors;

/// <summary>
/// Registers <c>ProblemDetails</c> generation with a <c>traceId</c> extension on every response (D12),
/// so a client can quote it and we can find the matching log line.
/// </summary>
public static class ProblemDetailsRegistration
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);

        return services;
    }
}
