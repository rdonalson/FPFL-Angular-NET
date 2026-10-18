using Fpfl.Items.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fpfl.Items.Infrastructure.Persistence.Configurations;

/// <summary>
///     Maps <see cref="Item" /> to <c>fpfl.items</c>, matching API-JDK's JPA entity column for column.
/// </summary>
public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items", ItemsDbContext.Schema);

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(i => i.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(i => i.Name).HasColumnName("name");
        builder.Property(i => i.Amount).HasColumnName("amount");

        builder.Property(i => i.ItemTypeId).HasColumnName("fk_item_type").IsRequired();
        builder.HasOne(i => i.ItemType).WithMany().HasForeignKey(i => i.ItemTypeId);

        builder.Property(i => i.TimePeriodId).HasColumnName("fk_time_period");
        builder.HasOne(i => i.TimePeriod).WithMany().HasForeignKey(i => i.TimePeriodId);

        builder.Property(i => i.BeginDate).HasColumnName("begin_date");
        builder.Property(i => i.EndDate).HasColumnName("end_date");
        builder.Property(i => i.WeeklyDow).HasColumnName("weekly_dow");
        builder.Property(i => i.EveryOtherWeekDow).HasColumnName("every_other_week_dow");
        builder.Property(i => i.BiMonthlyDay1).HasColumnName("bi_monthly_day_1");
        builder.Property(i => i.BiMonthlyDay2).HasColumnName("bi_monthly_day_2");
        builder.Property(i => i.MonthlyDom).HasColumnName("monthly_dom");
        builder.Property(i => i.Quarterly1Month).HasColumnName("quarterly_1_month");
        builder.Property(i => i.Quarterly1Day).HasColumnName("quarterly_1_day");
        builder.Property(i => i.Quarterly2Month).HasColumnName("quarterly_2_month");
        builder.Property(i => i.Quarterly2Day).HasColumnName("quarterly_2_day");
        builder.Property(i => i.Quarterly3Month).HasColumnName("quarterly_3_month");
        builder.Property(i => i.Quarterly3Day).HasColumnName("quarterly_3_day");
        builder.Property(i => i.Quarterly4Month).HasColumnName("quarterly_4_month");
        builder.Property(i => i.Quarterly4Day).HasColumnName("quarterly_4_day");
        builder.Property(i => i.SemiAnnual1Month).HasColumnName("semi_annual_1_month");
        builder.Property(i => i.SemiAnnual1Day).HasColumnName("semi_annual_1_day");
        builder.Property(i => i.SemiAnnual2Month).HasColumnName("semi_annual_2_month");
        builder.Property(i => i.SemiAnnual2Day).HasColumnName("semi_annual_2_day");
        builder.Property(i => i.AnnualMoy).HasColumnName("annual_moy");
        builder.Property(i => i.AnnualDom).HasColumnName("annual_dom");
        builder.Property(i => i.NthDow).HasColumnName("nth_dow");
        builder.Property(i => i.NthIndex).HasColumnName("nth_index");
        builder.Property(i => i.DateRangeReq).HasColumnName("date_range_req").IsRequired();
    }
}
