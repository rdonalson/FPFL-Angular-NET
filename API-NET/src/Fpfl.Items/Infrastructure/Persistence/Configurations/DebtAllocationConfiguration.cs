using Fpfl.Items.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fpfl.Items.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="DebtAllocation" /> to <c>fpfl.debt_allocations</c>.</summary>
public sealed class DebtAllocationConfiguration : IEntityTypeConfiguration<DebtAllocation>
{
    public void Configure(EntityTypeBuilder<DebtAllocation> builder)
    {
        builder.ToTable("debt_allocations", ItemsDbContext.Schema);

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(a => a.DebtId).HasColumnName("fk_debt").IsRequired();
        builder.Property(a => a.CreditItemId).HasColumnName("fk_credit_item").IsRequired();
        builder.HasOne(a => a.CreditItem).WithMany().HasForeignKey(a => a.CreditItemId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(a => a.Percent).HasColumnName("percent").HasPrecision(5, 2).IsRequired();
    }
}
