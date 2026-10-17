namespace Fpfl.Display.Domain.Models;

/// <summary>
///     One occurrence of a credit or debit on a ledger day. Port of API-JDK's <c>ItemDto</c>,
///     plus <see cref="ItemId" /> so the UI can link back to the item.
/// </summary>
public sealed record LedgerEntry
{
    /// <summary>1-based position within the day (API-JDK's itemKey).</summary>
    public required int ItemKey { get; init; }

    public required long ItemId { get; init; }

    public required DateOnly OccurrenceDate { get; init; }

    public required long ItemTypeId { get; init; }

    public string? ItemTypeName { get; init; }

    public string? PeriodName { get; init; }

    public string? Name { get; init; }

    /// <summary>Signed: credits are positive, debits negative.</summary>
    public required decimal Amount { get; init; }
}
