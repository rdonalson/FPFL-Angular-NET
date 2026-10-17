namespace Fpfl.Display.Domain.Models;

/// <summary>
///     One row of the forecast ledger: a single calendar day with its credits, debits and the
///     balance at the end of that day. Port of API-JDK's <c>LedgerDto</c>.
/// </summary>
public sealed class LedgerDay
{
    /// <summary>1-based position of the day in the ledger (API-JDK's rollupKey).</summary>
    public required int RollupKey { get; init; }

    public required DateOnly Date { get; init; }

    public int Year => Date.Year;

    /// <summary>Sum of the day's credits (zero or positive).</summary>
    public decimal CreditSummary { get; set; }

    /// <summary>Sum of the day's debits (zero or negative).</summary>
    public decimal DebitSummary { get; set; }

    /// <summary>CreditSummary + DebitSummary.</summary>
    public decimal Net { get; set; }

    /// <summary>Balance at the end of the day: the initial amount plus every Net up to and including this day.</summary>
    public decimal RunningTotal { get; set; }

    public List<LedgerEntry> Entries { get; } = [];
}
