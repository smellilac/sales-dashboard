using Microsoft.EntityFrameworkCore;
using SalesDashboard.Api.Data.Entities;

namespace SalesDashboard.Api.Data;

/// <summary>
/// EF Core context for the sales domain. Read-only at runtime (READ-ONLY invariant); the only writes are
/// migrations and seed via <see cref="SalesDbInitializer"/>.
/// </summary>
public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Manager> Managers => Set<Manager>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
