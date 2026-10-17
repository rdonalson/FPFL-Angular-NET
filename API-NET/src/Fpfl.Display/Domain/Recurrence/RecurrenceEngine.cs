using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>
///     Routes each item to the expander for its time period. API-JDK ran all ten expanders over every
///     item and let each ignore the ones that weren't its own; this looks the expander up once per item.
/// </summary>
public static class RecurrenceEngine
{
    private static readonly IReadOnlyDictionary<long, IRecurrenceExpander> Expanders =
        new IRecurrenceExpander[]
        {
            new OneTimeExpander(),
            new DailyExpander(),
            new WeeklyExpander(),
            new EveryTwoWeeksExpander(),
            new BiMonthlyExpander(),
            new MonthlyExpander(),
            new QuarterlyExpander(),
            new SemiAnnualExpander(),
            new AnnualExpander(),
            new NthWeekdayExpander(),
        }.ToDictionary(e => e.PeriodId);

    /// <summary>Every date <paramref name="item" /> occurs on within the ledger window, in order.</summary>
    public static IEnumerable<DateOnly> Occurrences(Item item, DateWindow ledger)
    {
        if (item.TimePeriodId is not { } periodId
            || !Expanders.TryGetValue(periodId, out var expander)
            || RecurrenceWindow.Resolve(item, ledger) is not { } window)
        {
            return [];
        }

        return expander.Expand(item, window).Order();
    }
}
