using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDashboard.Api.Data.Entities;

namespace SalesDashboard.Api.Data.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.HasOne(s => s.Manager)
            .WithMany(m => m.Sales)
            .HasForeignKey(s => s.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // CHECK list is built from the enum, not duplicated by hand (ENUMS).
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_sales_status",
            $"status IN ({string.Join(", ", Enum.GetNames<SaleStatus>().Select(n => $"'{n}'"))})"));
    }
}
