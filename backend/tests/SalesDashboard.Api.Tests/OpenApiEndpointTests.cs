using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>Verifies the OpenAPI document is served at runtime (OPENAPI).</summary>
public sealed class OpenApiEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
