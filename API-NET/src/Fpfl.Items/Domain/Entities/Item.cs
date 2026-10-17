namespace Fpfl.Items.Domain.Entities;

/// <summary>
///     A user's recurring (or one-time) credit or debit. Maps to <c>fpfl.items</c>.
///     Only the recurrence fields that match <see cref="TimePeriodId" /> are populated;
///     the ledger reads exactly that set when expanding occurrences.
/// </summary>
public sealed class Item
{
    public long Id { get; set; }

    /// <summary>Owner: <c>users.user_id</c> (UUID), the same value carried in the access token.</summary>
    public Guid UserId { get; set; }

    /// <summary>Nullable in the shared schema (API-JDK allows it).</summary>
    public string? Name { get; set; }

    /// <summary>Always positive; the item type says whether it is a credit or a debit. Nullable in the shared schema.</summary>
    public double? Amount { get; set; }

    public long ItemTypeId { get; set; }

    public ItemType? ItemType { get; set; }

    public long? TimePeriodId { get; set; }

    public TimePeriod? TimePeriod { get; set; }

    public DateOnly? BeginDate { get; set; }

    public DateOnly? EndDate { get; set; }

    /// <summary>Weekly: day of week, 0 = Sunday … 6 = Saturday.</summary>
    public int? WeeklyDow { get; set; }

    /// <summary>Every two weeks: day of week, 0 = Sunday … 6 = Saturday.</summary>
    public int? EveryOtherWeekDow { get; set; }

    public int? BiMonthlyDay1 { get; set; }

    public int? BiMonthlyDay2 { get; set; }

    public int? MonthlyDom { get; set; }

    public int? Quarterly1Month { get; set; }

    public int? Quarterly1Day { get; set; }

    public int? Quarterly2Month { get; set; }

    public int? Quarterly2Day { get; set; }

    public int? Quarterly3Month { get; set; }

    public int? Quarterly3Day { get; set; }

    public int? Quarterly4Month { get; set; }

    public int? Quarterly4Day { get; set; }

    public int? SemiAnnual1Month { get; set; }

    public int? SemiAnnual1Day { get; set; }

    public int? SemiAnnual2Month { get; set; }

    public int? SemiAnnual2Day { get; set; }

    /// <summary>Annual: month of year, 1 = January.</summary>
    public int? AnnualMoy { get; set; }

    public int? AnnualDom { get; set; }

    /// <summary>Nth weekday: day of week, 0 = Sunday … 6 = Saturday.</summary>
    public int? NthDow { get; set; }

    /// <summary>Nth weekday: 1–4 for 1st–4th, -1 for last.</summary>
    public int? NthIndex { get; set; }

    public bool DateRangeReq { get; set; }
}
