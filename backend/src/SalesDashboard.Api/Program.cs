using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SalesDashboard.Api.Data;
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

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<SalesDbContext>(tags: ["ready"]);

    var app = builder.Build();

    // Apply pending migrations at startup when enabled (DB-INIT). This is the only place the API runs DDL.
    var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (databaseOptions.ApplyMigrationsOnStartup)
    {
        var scope = app.Services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            await dbContext.Database.MigrateAsync(app.Lifetime.ApplicationStopping).ConfigureAwait(false);
        }
    }

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
