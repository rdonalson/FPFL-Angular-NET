using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Once a month on the 1st–4th or last weekday of the month (e.g. the 2nd Tuesday).</summary>
public sealed class NthWeekdayExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.NthWeekday;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window)
    {
        if (CalendarMath.ToDayOfWeek(item.NthDow) is not { } dayOfWeek || item.NthIndex is not (>= 1 and <= 4 or -1))
        {
            yield break;
        }

        foreach (var (year, month) in CalendarMath.MonthsIn(window))
        {
            var date = CalendarMath.NthWeekdayOfMonth(year, month, dayOfWeek, item.NthIndex.Value);
            if (window.Contains(date))
            {
                yield return date;
            }
        }
    }
}
