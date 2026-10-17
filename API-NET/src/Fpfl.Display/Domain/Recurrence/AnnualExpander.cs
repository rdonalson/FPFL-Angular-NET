using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>Once a year on a month/day (Feb 29 falls on Feb 28 outside leap years).</summary>
public sealed class AnnualExpander : IRecurrenceExpander
{
    public long PeriodId => ItemRules.Annual;

    public IEnumerable<DateOnly> Expand(Item item, DateWindow window) =>
        YearlyAnchors.Expand(window, (item.AnnualMoy, item.AnnualDom));
}
