namespace SalesDashboard.Api.Data;

/// <summary>Options for the <c>Database</c> configuration section (DB-INIT).</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// When true, the application applies pending migrations at startup (in Program.cs).
    /// Enabled only in docker-compose; off everywhere else so app code does not run DDL by default.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; init; }
}
