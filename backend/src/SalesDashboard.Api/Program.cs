using Dapper;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Data.Seed;
using SalesDashboard.Api.Features;
using SalesDashboard.Api.Shared;
using SalesDashboard.Api.Shared.Data;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Json;
using Serilog;

// Bootstrap logger: captures startup failures before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // Dapper does not support DateOnly parameters out of the box; register the handler once, before any query
    // runs, so timeseries (D10) can bind and read DateOnly for both parameters and results.
    SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());

    // One NpgsqlDataSource for the whole app (shared by EF Core now and Dapper later, D10).
    // Registered manually to avoid pulling Npgsql.DependencyInjection; built lazily on first resolve,
    // so an empty connection string does not fail startup for endpoints that never touch the database.
    builder.Services.AddSingleton(sp =>
    {
        var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Sales");
        return new NpgsqlDataSourceBuilder(connectionString).Build();
    });

    builder.Services.AddDbContext<SalesDbContext>((sp, options) => options
        .UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
        .UseSnakeCaseNamingConvention());

    builder.Services.AddOptions<DatabaseOptions>()
        .BindConfiguration(DatabaseOptions.SectionName)
        .ValidateOnStart();

    builder.Services.AddOptions<ReportingOptions>()
        .BindConfiguration(ReportingOptions.SectionName)
        .Validate(
            static options => TimeZoneInfo.TryFindSystemTimeZoneById(options.TimeZone, out _),
            "Reporting:TimeZone must be a time zone id resolvable on this system.")
        .ValidateOnStart();

    // TimeProvider (D4): the seed generator reads "now" through it; tests inject FakeTimeProvider.
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<SalesDataSeeder>();

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<SalesDbContext>(tags: ["ready"]);

    // Applies migrations then seeds at startup when enabled (DB-INIT). The only place the API runs DDL.
    builder.Services.AddHostedService<SalesDbInitializer>();

    // Source-generated JSON contracts first in the chain (D6: nulls written, enums as strings).
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

    // Expected errors -> ProblemDetails with traceId (D12); unhandled exceptions -> GlobalExceptionHandler.
    builder.Services.AddApiProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // OpenAPI document + Scalar UI (OPENAPI). Document is also emitted to backend/openapi/ at build time.
    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseExceptionHandler();

    app.UseSerilogRequestLogging();

    // Liveness: no dependency checks, returns 200 when the process is up. An empty predicate runs no checks,
    // so the DbContext readiness check does not leak in and fail liveness when the database is unreachable.
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
    });

    // Readiness: only checks tagged "ready" (the database is reachable).
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

    // OpenAPI JSON at /openapi/v1.json and the Scalar reference UI at /scalar (no auth, D8/OPENAPI).
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapDashboard();

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

// Exposed for WebApplicationFactory<Program> in the test project.
public partial class Program
{
    protected Program()
    {
    }
}
