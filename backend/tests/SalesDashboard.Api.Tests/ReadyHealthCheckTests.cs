using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SalesDashboard.Api.Tests;

public sealed class ReadyHealthCheckTests(PostgresFixture postgres)
{
    [Fact]
    public async Task ReadyEndpoint_ReturnsOk_WhenDatabaseAvailable()
    {
        var connectionString = await postgres.CreateDatabaseAsync();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Sales", connectionString)
                .UseSetting("Database:ApplyMigrationsOnStartup", "true"));

        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/health/ready", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
