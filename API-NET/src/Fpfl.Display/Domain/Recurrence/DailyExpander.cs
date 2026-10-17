using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Every day.</summary>
public sealed class DailyExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.Daily;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window)
    {
        for (var date = window.Start; date <= window.End; date = date.AddDays(1))
        {
            yield return date;
        }
    }
}
