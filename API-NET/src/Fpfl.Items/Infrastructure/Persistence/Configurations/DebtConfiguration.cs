using Fpfl.Items.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fpfl.Items.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Debt" /> to <c>fpfl.debts</c> (see <c>db/001_debt_paydown.sql</c>).</summary>
public sealed class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.ToTable("debts", ItemsDbContext.Schema);

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(d => d.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(d => d.ItemId).HasColumnName("fk_item").IsRequired();
        builder.HasOne(d => d.Item).WithMany().HasForeignKey(d => d.ItemId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(d => d.Balance).HasColumnName("balance").HasPrecision(14, 2).IsRequired();
        builder.Property(d => d.Apr).HasColumnName("apr").HasPrecision(6, 3).IsRequired();
        builder.Property(d => d.BalanceDate).HasColumnName("balance_date").IsRequired();

        builder.HasMany(d => d.Allocations).WithOne().HasForeignKey(a => a.DebtId).OnDelete(DeleteBehavior.Cascade);
    }
}
