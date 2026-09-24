using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDashboard.Api.Data.Entities;

namespace SalesDashboard.Api.Data.Configurations;

internal sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.UnitCost).HasPrecision(18, 2);

        // STORED generated columns keep the line formula in one place for EF Core and Dapper (D5).
        builder.Property(i => i.LineRevenue)
            .HasPrecision(18, 2)
            .HasComputedColumnSql("quantity * unit_price", stored: true);
        builder.Property(i => i.LineCost)
            .HasPrecision(18, 2)
            .HasComputedColumnSql("quantity * unit_cost", stored: true);

        builder.HasOne(i => i.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_sale_items_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_sale_items_unit_price", "unit_price >= 0");
            t.HasCheckConstraint("ck_sale_items_unit_cost", "unit_cost >= 0");
        });
    }
}
