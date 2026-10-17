using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Once, on the begin date.</summary>
public sealed class OneTimeExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.OneTime;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window)
    {
        if (item.BeginDate is { } date && window.Contains(date))
        {
            yield return date;
        }
    }
}
