using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>
///     Turns one item into the dates it occurs on. One implementation per time period, as in API-JDK's
///     <c>recurrence</c> package; <see cref="RecurrenceEngine" /> picks the right one by period id.
/// </summary>
public interface IRecurrenceExpander
{
    long PeriodId { get; }

    /// <param name="item">An item whose TimePeriodId is <see cref="PeriodId" />.</param>
    /// <param name="window">Already narrowed to the item's own date range (see <see cref="RecurrenceWindow" />).</param>
    /// <returns>Occurrence dates inside <paramref name="window" />; none when the item's schedule fields are missing.</returns>
    IEnumerable<DateOnly> Expand(Item item, DateWindow window);
}
