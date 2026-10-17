using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Twice a year, on two month/day pairs.</summary>
public sealed class SemiAnnualExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.SemiAnnual;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window) =>
        YearlyAnchors.Expand(window,
            (item.SemiAnnual1Month, item.SemiAnnual1Day),
            (item.SemiAnnual2Month, item.SemiAnnual2Day));
}
