using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Tests;

public sealed class ItemRulesTests
{
    private static Item Draft(long period, Action<Item>? configure = null)
    {
        var item = new Item
        {
            UserId = Guid.NewGuid(),
            Name = "  Salary  ",
            Amount = 1000,
            ItemTypeId = ItemRules.Credit,
            TimePeriodId = period,
        };
        configure?.Invoke(item);
        return item;
    }

    private static string Broken(Item draft) =>
        Assert.Throws<DomainValidationException>(() => ItemRules.Normalize(draft)).Message;

    [Fact]
    public void Trims_the_name_and_rounds_the_amount_to_cents()
    {
        var item = ItemRules.Normalize(Draft(ItemRules.Daily, i => i.Amount = 10.006));

        Assert.Equal("Salary", item.Name);
        Assert.Equal(10.01, item.Amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Name_is_required(string? name) =>
        Assert.Equal("Item Name is required and cannot be blank", Broken(Draft(ItemRules.Daily, i => i.Name = name)));

    [Fact]
    public void Name_is_limited_to_150_characters() =>
        Assert.Contains("at most 150", Broken(Draft(ItemRules.Daily, i => i.Name = new string('a', 151))));

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(double.NaN)]
    public void Amount_must_be_positive(double? amount) =>
        Assert.Contains("positive currency value", Broken(Draft(ItemRules.Daily, i => i.Amount = amount)));

    [Theory]
    [InlineData(0)]
    [InlineData(3)] // initial amount has its own endpoint
    public void Only_credits_and_debits(long itemType) =>
        Assert.Equal("ItemType must be 1 (credit) or 2 (debit)", Broken(Draft(ItemRules.Daily, i => i.ItemTypeId = itemType)));

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(11L)]
    public void Period_must_be_1_to_10(long? period) =>
        Assert.Equal("Time Period must be between 1 and 10", Broken(Draft(ItemRules.Daily, i => i.TimePeriodId = period)));

    [Fact]
    public void Fields_from_other_periods_are_cleared()
    {
        var item = ItemRules.Normalize(Draft(ItemRules.Monthly, i =>
        {
            i.MonthlyDom = 15;
            i.WeeklyDow = 3;          // leftover from a weekly version of this item
            i.AnnualMoy = 6;
            i.AnnualDom = 1;
        }));

        Assert.Equal(15, item.MonthlyDom);
        Assert.Null(item.WeeklyDow);
        Assert.Null(item.AnnualMoy);
        Assert.Null(item.AnnualDom);
    }

    [Fact]
    public void Weekly_needs_a_weekday_from_0_to_6()
    {
        Assert.Equal(5, ItemRules.Normalize(Draft(ItemRules.Weekly, i => i.WeeklyDow = 5)).WeeklyDow);
        Assert.Contains("0 = Sunday", Broken(Draft(ItemRules.Weekly, i => i.WeeklyDow = 7)));
        Assert.Contains("0 = Sunday", Broken(Draft(ItemRules.Weekly)));
    }

    [Fact]
    public void Bi_monthly_needs_two_different_days()
    {
        var item = ItemRules.Normalize(Draft(ItemRules.BiMonthly, i => { i.BiMonthlyDay1 = 1; i.BiMonthlyDay2 = 15; }));
        Assert.Equal((1, 15), (item.BiMonthlyDay1!.Value, item.BiMonthlyDay2!.Value));

        Assert.Equal("Bi-monthly day 1 and day 2 must be different",
            Broken(Draft(ItemRules.BiMonthly, i => { i.BiMonthlyDay1 = 15; i.BiMonthlyDay2 = 15; })));
        Assert.Contains("must be 1–31", Broken(Draft(ItemRules.BiMonthly, i => { i.BiMonthlyDay1 = 0; i.BiMonthlyDay2 = 15; })));
    }

    [Fact]
    public void Monthly_needs_a_day_of_month() =>
        Assert.Contains("Monthly day of month", Broken(Draft(ItemRules.Monthly, i => i.MonthlyDom = 32)));

    [Fact]
    public void Quarterly_needs_all_four_dates()
    {
        void Three(Item i)
        {
            (i.Quarterly1Month, i.Quarterly1Day) = (1, 15);
            (i.Quarterly2Month, i.Quarterly2Day) = (4, 15);
            (i.Quarterly3Month, i.Quarterly3Day) = (7, 15);
        }

        Assert.Equal("Quarter 4: month (1–12) and day are required", Broken(Draft(ItemRules.Quarterly, Three)));

        var item = ItemRules.Normalize(Draft(ItemRules.Quarterly, i => { Three(i); (i.Quarterly4Month, i.Quarterly4Day) = (10, 15); }));
        Assert.Equal(10, item.Quarterly4Month);
    }

    [Theory]
    [InlineData(2, 30)]
    [InlineData(4, 31)]
    [InlineData(13, 1)]
    public void Month_and_day_must_be_a_real_date(int month, int day)
    {
        var message = Broken(Draft(ItemRules.Annual, i => { i.AnnualMoy = month; i.AnnualDom = day; }));
        Assert.StartsWith("Annual date:", message);
    }

    [Fact]
    public void Feb_29_is_allowed_for_leap_years() =>
        Assert.Equal(29, ItemRules.Normalize(Draft(ItemRules.Annual, i => { i.AnnualMoy = 2; i.AnnualDom = 29; })).AnnualDom);

    [Fact]
    public void Semi_annual_needs_both_dates() =>
        Assert.StartsWith("Semi-annual date 2:",
            Broken(Draft(ItemRules.SemiAnnual, i => { i.SemiAnnual1Month = 1; i.SemiAnnual1Day = 1; })));

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Nth_weekday_accepts_1st_to_4th_and_last(int index) =>
        Assert.Equal(index, ItemRules.Normalize(Draft(ItemRules.NthWeekday, i => { i.NthDow = 5; i.NthIndex = index; })).NthIndex);

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(null)]
    public void Nth_weekday_rejects_other_occurrences(int? index) =>
        Assert.Contains("must be 1, 2, 3, 4, or -1",
            Broken(Draft(ItemRules.NthWeekday, i => { i.NthDow = 5; i.NthIndex = index; })));

    [Fact]
    public void One_time_items_keep_only_their_date()
    {
        var day = new DateOnly(2026, 4, 15);
        var item = ItemRules.Normalize(Draft(ItemRules.OneTime, i => { i.BeginDate = day; i.EndDate = day.AddDays(5); }));

        Assert.Equal(day, item.BeginDate);
        Assert.Null(item.EndDate);
        Assert.True(item.DateRangeReq);
        Assert.Contains("needs its date", Broken(Draft(ItemRules.OneTime)));
    }

    [Fact]
    public void Every_two_weeks_needs_a_start_date_but_the_end_is_optional()
    {
        var start = new DateOnly(2026, 11, 6);

        Assert.Contains("needs a start date", Broken(Draft(ItemRules.EveryTwoWeeks, i => i.EveryOtherWeekDow = 5)));

        var open = ItemRules.Normalize(Draft(ItemRules.EveryTwoWeeks, i => { i.EveryOtherWeekDow = 5; i.BeginDate = start; i.EndDate = start.AddYears(1); }));
        Assert.Equal(start, open.BeginDate);
        Assert.Null(open.EndDate); // no date range asked for, so the end is dropped

        var ranged = ItemRules.Normalize(Draft(ItemRules.EveryTwoWeeks, i =>
        {
            i.EveryOtherWeekDow = 5; i.BeginDate = start; i.EndDate = start.AddYears(1); i.DateRangeReq = true;
        }));
        Assert.Equal(start.AddYears(1), ranged.EndDate);
    }

    [Fact]
    public void A_date_range_needs_both_ends_in_order()
    {
        var jan = new DateOnly(2026, 1, 1);
        var dec = new DateOnly(2026, 12, 31);

        Assert.Equal("Please select both begin and end dates for the date range",
            Broken(Draft(ItemRules.Daily, i => { i.DateRangeReq = true; i.BeginDate = jan; })));
        Assert.Equal("Begin date must be before or equal to end date",
            Broken(Draft(ItemRules.Daily, i => { i.DateRangeReq = true; i.BeginDate = dec; i.EndDate = jan; })));

        var item = ItemRules.Normalize(Draft(ItemRules.Daily, i => { i.DateRangeReq = true; i.BeginDate = jan; i.EndDate = dec; }));
        Assert.Equal((jan, dec), (item.BeginDate!.Value, item.EndDate!.Value));
    }

    [Fact]
    public void Without_a_date_range_any_dates_are_dropped()
    {
        var item = ItemRules.Normalize(Draft(ItemRules.Daily, i => { i.BeginDate = new DateOnly(2026, 1, 1); i.EndDate = new DateOnly(2026, 2, 1); }));

        Assert.Null(item.BeginDate);
        Assert.Null(item.EndDate);
        Assert.False(item.DateRangeReq);
    }
}
