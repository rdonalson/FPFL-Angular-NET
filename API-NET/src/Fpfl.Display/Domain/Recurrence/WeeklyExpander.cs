using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Every week on one weekday.</summary>
public sealed class WeeklyExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.Weekly;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window)
    {
        if (CalendarMath.ToDayOfWeek(item.WeeklyDow) is not { } dayOfWeek)
        {
            yield break;
        }

        for (var date = CalendarMath.NextOnOrAfter(window.Start, dayOfWeek); date <= window.End; date = date.AddDays(7))
        {
            yield return date;
        }
    }
}
