using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SalesDashboard.Api.Data;
using SalesDashboard.Api.Data.Seed;
using SalesDashboard.Api.Shared;
using Serilog;

// Bootstrap logger: captures startup failures before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
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

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Liveness: no dependency checks, returns 200 when the process is up.
    app.MapHealthChecks("/health/live");

    // Readiness: only checks tagged "ready" (the database is reachable).
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

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
