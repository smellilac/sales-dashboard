using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDashboard.Api.Data.Entities;

namespace SalesDashboard.Api.Data.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Sku).HasMaxLength(32);
        builder.Property(p => p.Name).HasMaxLength(200);

        builder.Property(p => p.ListPrice).HasPrecision(18, 2);
        builder.Property(p => p.BaseCost).HasPrecision(18, 2);

        builder.HasIndex(p => p.Sku).IsUnique();

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_products_list_price", "list_price >= 0");
            t.HasCheckConstraint("ck_products_base_cost", "base_cost >= 0");
        });
    }
}
