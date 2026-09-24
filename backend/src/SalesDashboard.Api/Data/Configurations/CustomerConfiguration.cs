using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDashboard.Api.Data.Entities;

namespace SalesDashboard.Api.Data.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ContactName).HasMaxLength(100);
        builder.Property(c => c.Company).HasMaxLength(200);

        builder.Property(c => c.Segment)
            .HasConversion<string>()
            .HasMaxLength(16);

        // CHECK list is built from the enum, not duplicated by hand (ENUMS).
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_customers_segment",
            $"segment IN ({string.Join(", ", Enum.GetNames<CustomerSegment>().Select(n => $"'{n}'"))})"));
    }
}
