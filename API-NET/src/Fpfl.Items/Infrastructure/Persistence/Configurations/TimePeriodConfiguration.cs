using Fpfl.Items.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fpfl.Items.Infrastructure.Persistence.Configurations;

/// <summary>
///     Maps <see cref="TimePeriod" /> to <c>fpfl.time_periods</c>, matching API-JDK's JPA entity.
/// </summary>
public sealed class TimePeriodConfiguration : IEntityTypeConfiguration<TimePeriod>
{
    public void Configure(EntityTypeBuilder<TimePeriod> builder)
    {
        builder.ToTable("time_periods", ItemsDbContext.Schema);

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
    }
}
