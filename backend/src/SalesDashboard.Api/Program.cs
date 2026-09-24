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

    // Read but not used in T1 — proves configuration wiring only.
    _ = builder.Configuration.GetConnectionString("Sales");

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Liveness: no dependency checks, returns 200 when the process is up.
    app.MapHealthChecks("/health/live");

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
