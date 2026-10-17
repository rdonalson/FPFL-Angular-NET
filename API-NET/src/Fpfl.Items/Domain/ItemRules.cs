using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain;

/// <summary>
///     The rules a credit or debit must satisfy before it is saved, in one place instead of spread
///     across ten UI forms (as in UI-TSX). <see cref="Normalize" /> validates an item and returns a
///     clean copy in which only the fields belonging to its time period are set, because the ledger
///     reads exactly that set when expanding occurrences.
/// </summary>
public static class ItemRules
{
    public const long Credit = 1;
    public const long Debit = 2;

    public const long OneTime = 1;
    public const long Daily = 2;
    public const long Weekly = 3;
    public const long EveryTwoWeeks = 4;
    public const long BiMonthly = 5;
    public const long Monthly = 6;
    public const long Quarterly = 7;
    public const long SemiAnnual = 8;
    public const long Annual = 9;
    public const long NthWeekday = 10;

    public const int MaxNameLength = 150;
    public const double MaxAmount = 1_000_000_000;

    /// <summary>Days in each month, allowing Feb 29 so leap-year dates can be scheduled.</summary>
    private static readonly int[] DaysInMonth = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    /// <exception cref="DomainValidationException">Any rule is broken; the message says which.</exception>
    public static Item Normalize(Item draft)
    {
        var name = draft.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw Invalid("Item Name is required and cannot be blank");
        }
        if (name.Length > MaxNameLength)
        {
            throw Invalid($"Item Name must be at most {MaxNameLength} characters");
        }

        if (draft.Amount is not { } amount || !double.IsFinite(amount) || amount <= 0)
        {
            throw Invalid("Item Amount is required and must be a positive currency value");
        }
        if (amount > MaxAmount)
        {
            throw Invalid("Item Amount is too large");
        }

        if (draft.ItemTypeId is not (Credit or Debit))
        {
            throw Invalid("ItemType must be 1 (credit) or 2 (debit)");
        }

        if (draft.TimePeriodId is not { } period || period is < OneTime or > NthWeekday)
        {
            throw Invalid("Time Period must be between 1 and 10");
        }

        var item = new Item
        {
            Id = draft.Id,
            UserId = draft.UserId,
            Name = name,
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            ItemTypeId = draft.ItemTypeId,
            TimePeriodId = period,
        };

        ApplySchedule(draft, item, period);
        ApplyDates(draft, item, period);
        return item;
    }

    private static void ApplySchedule(Item draft, Item item, long period)
    {
        switch (period)
        {
            case Weekly:
                item.WeeklyDow = Weekday(draft.WeeklyDow, "Weekly day of week");
                break;
            case EveryTwoWeeks:
                item.EveryOtherWeekDow = Weekday(draft.EveryOtherWeekDow, "Every-two-weeks day of week");
                break;
            case BiMonthly:
                item.BiMonthlyDay1 = DayOfMonth(draft.BiMonthlyDay1, "Bi-monthly day 1");
                item.BiMonthlyDay2 = DayOfMonth(draft.BiMonthlyDay2, "Bi-monthly day 2");
                if (item.BiMonthlyDay1 == item.BiMonthlyDay2)
                {
                    throw Invalid("Bi-monthly day 1 and day 2 must be different");
                }
                break;
            case Monthly:
                item.MonthlyDom = DayOfMonth(draft.MonthlyDom, "Monthly day of month");
                break;
            case Quarterly:
                (item.Quarterly1Month, item.Quarterly1Day) = MonthAndDay(draft.Quarterly1Month, draft.Quarterly1Day, "Quarter 1");
                (item.Quarterly2Month, item.Quarterly2Day) = MonthAndDay(draft.Quarterly2Month, draft.Quarterly2Day, "Quarter 2");
                (item.Quarterly3Month, item.Quarterly3Day) = MonthAndDay(draft.Quarterly3Month, draft.Quarterly3Day, "Quarter 3");
                (item.Quarterly4Month, item.Quarterly4Day) = MonthAndDay(draft.Quarterly4Month, draft.Quarterly4Day, "Quarter 4");
                break;
            case SemiAnnual:
                (item.SemiAnnual1Month, item.SemiAnnual1Day) = MonthAndDay(draft.SemiAnnual1Month, draft.SemiAnnual1Day, "Semi-annual date 1");
                (item.SemiAnnual2Month, item.SemiAnnual2Day) = MonthAndDay(draft.SemiAnnual2Month, draft.SemiAnnual2Day, "Semi-annual date 2");
                break;
            case Annual:
                (item.AnnualMoy, item.AnnualDom) = MonthAndDay(draft.AnnualMoy, draft.AnnualDom, "Annual date");
                break;
            case NthWeekday:
                item.NthDow = Weekday(draft.NthDow, "Nth-weekday day of week");
                if (draft.NthIndex is not (1 or 2 or 3 or 4 or -1))
                {
                    throw Invalid("Nth-weekday occurrence must be 1, 2, 3, 4, or -1 (last)");
                }
                item.NthIndex = draft.NthIndex;
                break;
        }
    }

    private static void ApplyDates(Item draft, Item item, long period)
    {
        switch (period)
        {
            case OneTime:
                // A one-time item happens on its begin date; it has no end.
                item.BeginDate = draft.BeginDate ?? throw Invalid("A one-time item needs its date (beginDate)");
                item.EndDate = null;
                item.DateRangeReq = true;
                return;

            case EveryTwoWeeks:
                // The begin date anchors which weeks count; the end date is optional.
                item.BeginDate = draft.BeginDate ?? throw Invalid("An every-two-weeks item needs a start date (beginDate)");
                item.DateRangeReq = draft.DateRangeReq;
                if (draft.DateRangeReq)
                {
                    item.EndDate = draft.EndDate ?? throw Invalid("Please select both begin and end dates for the date range");
                }
                break;

            default:
                item.DateRangeReq = draft.DateRangeReq;
                if (draft.DateRangeReq)
                {
                    if (draft.BeginDate is null || draft.EndDate is null)
                    {
                        throw Invalid("Please select both begin and end dates for the date range");
                    }
                    item.BeginDate = draft.BeginDate;
                    item.EndDate = draft.EndDate;
                }
                break;
        }

        if (item.BeginDate is { } begin && item.EndDate is { } end && begin > end)
        {
            throw Invalid("Begin date must be before or equal to end date");
        }
    }

    private static int Weekday(int? value, string label) =>
        value is >= 0 and <= 6 ? value.Value : throw Invalid($"{label} is required (0 = Sunday … 6 = Saturday)");

    private static int DayOfMonth(int? value, string label) =>
        value is >= 1 and <= 31 ? value.Value : throw Invalid($"{label} is required and must be 1–31");

    private static (int Month, int Day) MonthAndDay(int? month, int? day, string label)
    {
        if (month is not (>= 1 and <= 12) || day is null)
        {
            throw Invalid($"{label}: month (1–12) and day are required");
        }
        if (day < 1 || day > DaysInMonth[month.Value - 1])
        {
            throw Invalid($"{label}: day {day} is not valid for month {month}");
        }
        return (month.Value, day.Value);
    }

    private static DomainValidationException Invalid(string message) => new(message);
}
