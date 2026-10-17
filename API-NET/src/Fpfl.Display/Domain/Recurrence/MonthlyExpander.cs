using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Once a month on one day of the month (the 31st becomes the month's last day).</summary>
public sealed class MonthlyExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.Monthly;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window)
    {
        if (item.MonthlyDom is not { } day)
        {
            yield break;
        }

        foreach (var (year, month) in CalendarMath.MonthsIn(window))
        {
            var date = CalendarMath.ClampedDate(year, month, day);
            if (window.Contains(date))
            {
                yield return date;
            }
        }
    }
}
