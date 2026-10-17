using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Twice a month, on two days of the month (e.g. the 15th and the 31st, which becomes the month's last day).</summary>
public sealed class BiMonthlyExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.BiMonthly;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window)
    {
        int?[] days = [item.BiMonthlyDay1, item.BiMonthlyDay2];

        foreach (var (year, month) in CalendarMath.MonthsIn(window))
        {
            foreach (var day in days)
            {
                if (day is null)
                {
                    continue;
                }

                var date = CalendarMath.ClampedDate(year, month, day.Value);
                if (window.Contains(date))
                {
                    yield return date;
                }
            }
        }
    }
}
