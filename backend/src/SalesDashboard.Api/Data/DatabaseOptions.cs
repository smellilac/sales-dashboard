namespace SalesDashboard.Api.Data;

/// <summary>Options for the <c>Database</c> configuration section (DB-INIT).</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// When true, <see cref="SalesDbInitializer"/> applies pending migrations at startup.
    /// Enabled only in docker-compose; off everywhere else so app code does not run DDL by default.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; init; }
}
