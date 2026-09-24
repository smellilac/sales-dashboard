using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>
/// Integration test for <c>GlobalExceptionHandler</c> (D12). A test-only route that throws is injected
/// through an <see cref="IStartupFilter"/>; the response must be a 500 ProblemDetails carrying a
/// <c>traceId</c> and no exception details (the factory runs in Production).
/// </summary>
public sealed class GlobalExceptionHandlerTests
{
    private const string ThrowPath = "/__throw";
    private const string ThrowMessage = "Boom from the test route.";

    [Fact]
    public async Task UnhandledException_Returns500ProblemDetails_WithTraceId_WithoutStackTrace()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, ThrowingRouteStartupFilter>());
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri(ThrowPath, UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("traceId", body, StringComparison.Ordinal);
        // Production must not leak the exception message or a stack trace to the client.
        Assert.DoesNotContain(ThrowMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Appends a terminal middleware after the application's own pipeline (including the exception
    /// handler) that throws for <see cref="ThrowPath"/>, giving the handler something to catch.
    /// </summary>
    private sealed class ThrowingRouteStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Use(async (context, nextMiddleware) =>
            {
                if (string.Equals(context.Request.Path, ThrowPath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(ThrowMessage);
                }

                await nextMiddleware(context).ConfigureAwait(false);
            });
        };
    }
}
