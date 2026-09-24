using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SalesDashboard.Api.Data;

/// <summary>
/// Design-time factory for EF Core tools (<c>dotnet ef migrations</c>). The runtime host builds the
/// context from a shared <see cref="Npgsql.NpgsqlDataSource"/>, which needs a real connection string;
/// tooling only needs the model, so a placeholder connection string is enough — no database is contacted
/// when adding or scripting a migration.
/// </summary>
internal sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=sales;Username=sales;Password=sales";

    public SalesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(DesignTimeConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SalesDbContext(options);
    }
}
