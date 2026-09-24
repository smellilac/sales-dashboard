using Microsoft.AspNetCore.Mvc.Testing;

namespace SalesDashboard.Api.Tests.Dashboard;

/// <summary>A started API and its <see cref="HttpClient"/>; disposing tears down the host.</summary>
internal sealed class DashboardApp(WebApplicationFactory<Program> factory) : IAsyncDisposable
{
    public HttpClient Client { get; } = factory.CreateClient();

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await factory.DisposeAsync();
    }
}
