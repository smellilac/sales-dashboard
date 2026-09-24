using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDashboard.Api.Data.Entities;

namespace SalesDashboard.Api.Data.Configurations;

internal sealed class ManagerConfiguration : IEntityTypeConfiguration<Manager>
{
    public void Configure(EntityTypeBuilder<Manager> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.FirstName).HasMaxLength(100);
        builder.Property(m => m.LastName).HasMaxLength(100);
        builder.Property(m => m.Team).HasMaxLength(100);
        builder.Property(m => m.Position).HasMaxLength(100);
    }
}
