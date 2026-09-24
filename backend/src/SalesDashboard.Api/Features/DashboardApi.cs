using SalesDashboard.Api.Features.Kpi;
using SalesDashboard.Api.Features.ManagerRanking;

namespace SalesDashboard.Api.Features;

/// <summary>
/// Root of the dashboard HTTP surface. Each block is a vertical slice (D8, D9) that maps itself onto the
/// shared <c>/api/dashboard</c> group via its own static <c>Map(RouteGroupBuilder)</c> method.
/// </summary>
public static class DashboardApi
{
    public static RouteGroupBuilder MapDashboard(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapKpis();
        group.MapManagerRanking();

        return group;
    }
}
