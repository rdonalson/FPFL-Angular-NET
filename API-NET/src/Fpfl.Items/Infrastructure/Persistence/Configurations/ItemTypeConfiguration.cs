using Fpfl.Items.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fpfl.Items.Infrastructure.Persistence.Configurations;

/// <summary>
///     Maps <see cref="ItemType" /> to <c>fpfl.item_types</c>, matching API-JDK's JPA entity.
/// </summary>
public sealed class ItemTypeConfiguration : IEntityTypeConfiguration<ItemType>
{
    public void Configure(EntityTypeBuilder<ItemType> builder)
    {
        builder.ToTable("item_types", ItemsDbContext.Schema);

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
    }
}
