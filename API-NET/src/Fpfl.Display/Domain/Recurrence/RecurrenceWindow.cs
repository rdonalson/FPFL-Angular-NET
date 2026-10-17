using Fpfl.Items.Domain.Entities;

namespace Fpfl.Display.Domain.Recurrence;

/// <summary>
///     Port of API-JDK's <c>RecurrenceRange</c>: the days an item may occur on, given the ledger's window.
/// </summary>
public static class RecurrenceWindow
{
    /// <summary>
    ///     Without a date range the item runs for the whole ledger. With one, it runs only where its
    ///     begin/end overlap the ledger; a missing end means "open-ended". Null when there is no overlap.
    /// </summary>
    public static DateWindow? Resolve(Item item, DateWindow ledger)
    {
        if (!item.DateRangeReq)
        {
            return ledger;
        }

        return ledger.Intersect(item.BeginDate ?? ledger.Start, item.EndDate ?? ledger.End);
    }
}
