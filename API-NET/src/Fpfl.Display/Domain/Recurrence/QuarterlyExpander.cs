using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Four times a year, on four month/day pairs.</summary>
public sealed class QuarterlyExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.Quarterly;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window) =>
        YearlyAnchors.Expand(window,
            (item.Quarterly1Month, item.Quarterly1Day),
            (item.Quarterly2Month, item.Quarterly2Day),
            (item.Quarterly3Month, item.Quarterly3Day),
            (item.Quarterly4Month, item.Quarterly4Day));
}
