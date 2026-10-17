using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>
///     Every other week on one weekday, counted from the first such weekday on or after the begin date.
///     API-JDK counted from the ledger's start instead, so the paydays shifted by a week depending on
///     which start date you picked; anchoring on the begin date keeps them fixed.
/// </summary>
public sealed class EveryTwoWeeksExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.EveryTwoWeeks;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window)
    {
        if (CalendarMath.ToDayOfWeek(item.EveryOtherWeekDow) is not { } dayOfWeek)
        {
            yield break;
        }

        var date = CalendarMath.NextOnOrAfter(item.BeginDate ?? window.Start, dayOfWeek);
        if (date < window.Start)
        {
            // Jump straight to the first occurrence inside the window.
            var periods = (window.Start.DayNumber - date.DayNumber + 13) / 14;
            date = date.AddDays(14 * periods);
        }

        for (; date <= window.End; date = date.AddDays(14))
        {
            yield return date;
        }
    }
}
