using Fpfl.Display.Domain.Recurrence;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Tests;

/// <summary>One test (or more) per time period. Calendar anchors: 2026-10-01 is a Thursday, 2026 is not a leap year, 2028 is.</summary>
public sealed class RecurrenceEngineTests
{
    private static DateOnly D(int year, int month, int day) => new(year, month, day);

    private static DateWindow Window(DateOnly start, DateOnly end) => new(start, end);

    private static Item MakeItem(long periodId, Action<Item>? configure = null)
    {
        var item = new Item { Id = 1, ItemTypeId = ItemRules.Debit, TimePeriodId = periodId, Amount = 10 };
        configure?.Invoke(item);
        return item;
    }

    private static DateOnly[] Dates(Item item, DateWindow window) => RecurrenceEngine.Occurrences(item, window).ToArray();

    [Fact]
    public void One_time_occurs_on_its_begin_date_only_inside_the_window()
    {
        var item = MakeItem(ItemRules.OneTime, i => { i.BeginDate = D(2026, 10, 15); i.DateRangeReq = true; });

        Assert.Equal(new[] { D(2026, 10, 15) }, Dates(item, Window(D(2026, 10, 1), D(2026, 10, 31))));
        Assert.Empty(Dates(item, Window(D(2026, 11, 1), D(2026, 11, 30))));
    }

    [Fact]
    public void Daily_occurs_every_day()
    {
        Assert.Equal(
            new[] { D(2026, 10, 1), D(2026, 10, 2), D(2026, 10, 3) },
            Dates(MakeItem(ItemRules.Daily), Window(D(2026, 10, 1), D(2026, 10, 3))));
    }

    [Fact]
    public void Weekly_handles_sunday_as_zero()
    {
        var item = MakeItem(ItemRules.Weekly, i => i.WeeklyDow = 0);

        Assert.Equal(
            new[] { D(2026, 10, 4), D(2026, 10, 11), D(2026, 10, 18), D(2026, 10, 25) },
            Dates(item, Window(D(2026, 10, 1), D(2026, 10, 31))));
    }

    [Fact]
    public void Every_two_weeks_stays_anchored_on_the_begin_date()
    {
        // Paid on alternate Fridays starting Fri Oct 2: Oct 2, 16, 30, Nov 13 …
        var item = MakeItem(ItemRules.EveryTwoWeeks, i => { i.EveryOtherWeekDow = 5; i.BeginDate = D(2026, 10, 2); });

        // A ledger starting Oct 20 must not shift the cadence to Oct 23 / Nov 6.
        Assert.Equal(new[] { D(2026, 10, 30), D(2026, 11, 13) }, Dates(item, Window(D(2026, 10, 20), D(2026, 11, 20))));
    }

    [Fact]
    public void Every_two_weeks_starts_on_the_first_matching_weekday_after_the_begin_date()
    {
        // Begin Thu Oct 1, weekday Friday → first payday Fri Oct 2.
        var item = MakeItem(ItemRules.EveryTwoWeeks, i => { i.EveryOtherWeekDow = 5; i.BeginDate = D(2026, 10, 1); });

        Assert.Equal(new[] { D(2026, 10, 2), D(2026, 10, 16) }, Dates(item, Window(D(2026, 9, 1), D(2026, 10, 20))));
    }

    [Fact]
    public void Bi_monthly_clamps_the_second_day_to_the_end_of_short_months()
    {
        var item = MakeItem(ItemRules.BiMonthly, i => { i.BiMonthlyDay1 = 15; i.BiMonthlyDay2 = 31; });

        Assert.Equal(
            new[] { D(2026, 2, 15), D(2026, 2, 28), D(2026, 3, 15), D(2026, 3, 31) },
            Dates(item, Window(D(2026, 2, 1), D(2026, 3, 31))));
    }

    [Fact]
    public void Monthly_on_the_31st_lands_on_each_months_last_day()
    {
        var item = MakeItem(ItemRules.Monthly, i => i.MonthlyDom = 31);

        Assert.Equal(
            new[] { D(2026, 1, 31), D(2026, 2, 28), D(2026, 3, 31), D(2026, 4, 30) },
            Dates(item, Window(D(2026, 1, 1), D(2026, 4, 30))));
    }

    [Fact]
    public void Monthly_skips_a_day_before_the_window_starts()
    {
        var item = MakeItem(ItemRules.Monthly, i => i.MonthlyDom = 1);

        Assert.Equal(new[] { D(2026, 11, 1) }, Dates(item, Window(D(2026, 10, 17), D(2026, 11, 16))));
    }

    [Fact]
    public void Quarterly_crosses_into_the_next_year()
    {
        var item = MakeItem(ItemRules.Quarterly, i =>
        {
            (i.Quarterly1Month, i.Quarterly1Day) = (1, 15);
            (i.Quarterly2Month, i.Quarterly2Day) = (4, 15);
            (i.Quarterly3Month, i.Quarterly3Day) = (7, 15);
            (i.Quarterly4Month, i.Quarterly4Day) = (10, 15);
        });

        Assert.Equal(
            new[] { D(2026, 4, 15), D(2026, 7, 15), D(2026, 10, 15), D(2027, 1, 15) },
            Dates(item, Window(D(2026, 3, 1), D(2027, 1, 31))));
    }

    [Fact]
    public void Semi_annual_occurs_twice_a_year_in_date_order()
    {
        // Anchors given out of order still come back sorted.
        var item = MakeItem(ItemRules.SemiAnnual, i =>
        {
            (i.SemiAnnual1Month, i.SemiAnnual1Day) = (12, 31);
            (i.SemiAnnual2Month, i.SemiAnnual2Day) = (6, 30);
        });

        Assert.Equal(new[] { D(2026, 6, 30), D(2026, 12, 31) }, Dates(item, Window(D(2026, 1, 1), D(2026, 12, 31))));
    }

    [Fact]
    public void Annual_on_feb_29_falls_on_feb_28_outside_leap_years()
    {
        var item = MakeItem(ItemRules.Annual, i => { i.AnnualMoy = 2; i.AnnualDom = 29; });

        Assert.Equal(new[] { D(2027, 2, 28), D(2028, 2, 29) }, Dates(item, Window(D(2027, 1, 1), D(2028, 12, 31))));
    }

    [Fact]
    public void Nth_weekday_finds_the_second_tuesday()
    {
        var item = MakeItem(ItemRules.NthWeekday, i => { i.NthDow = 2; i.NthIndex = 2; });

        Assert.Equal(new[] { D(2026, 10, 13), D(2026, 11, 10) }, Dates(item, Window(D(2026, 10, 1), D(2026, 11, 30))));
    }

    [Fact]
    public void Nth_weekday_minus_one_is_the_last_one_of_the_month()
    {
        var item = MakeItem(ItemRules.NthWeekday, i => { i.NthDow = 5; i.NthIndex = -1; });

        Assert.Equal(new[] { D(2026, 10, 30), D(2026, 11, 27) }, Dates(item, Window(D(2026, 10, 1), D(2026, 11, 30))));
    }

    [Fact]
    public void An_items_date_range_narrows_the_window()
    {
        var item = MakeItem(ItemRules.Monthly, i =>
        {
            i.MonthlyDom = 1;
            i.DateRangeReq = true;
            i.BeginDate = D(2026, 11, 1);
            i.EndDate = D(2026, 12, 31);
        });

        Assert.Equal(new[] { D(2026, 11, 1), D(2026, 12, 1) }, Dates(item, Window(D(2026, 10, 1), D(2027, 1, 31))));
    }

    [Fact]
    public void A_date_range_outside_the_ledger_produces_nothing()
    {
        var item = MakeItem(ItemRules.Daily, i =>
        {
            i.DateRangeReq = true;
            i.BeginDate = D(2025, 1, 1);
            i.EndDate = D(2025, 12, 31);
        });

        Assert.Empty(Dates(item, Window(D(2026, 1, 1), D(2026, 1, 31))));
    }

    [Fact]
    public void Missing_schedule_fields_produce_nothing()
    {
        var window = Window(D(2026, 1, 1), D(2026, 12, 31));

        Assert.Empty(Dates(MakeItem(ItemRules.Weekly), window));
        Assert.Empty(Dates(MakeItem(ItemRules.Monthly), window));
        Assert.Empty(Dates(MakeItem(ItemRules.NthWeekday, i => { i.NthDow = 1; i.NthIndex = 5; }), window));
        Assert.Empty(Dates(MakeItem(ItemRules.Weekly, i => i.WeeklyDow = 7), window));
        Assert.Empty(Dates(new Item { TimePeriodId = null }, window));
    }
}
