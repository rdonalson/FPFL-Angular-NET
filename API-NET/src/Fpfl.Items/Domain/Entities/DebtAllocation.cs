namespace Fpfl.Items.Domain.Entities;

/// <summary>
///     A share of a credit sent to a debt every time the credit occurs, e.g. 30% of each paycheck.
///     Maps to <c>fpfl.debt_allocations</c>.
/// </summary>
public sealed class DebtAllocation
{
    public long Id { get; set; }

    public long DebtId { get; set; }

    /// <summary>The credit the money comes from.</summary>
    public long CreditItemId { get; set; }

    public Item? CreditItem { get; set; }

    /// <summary>Share of each occurrence of the credit, above 0 and at most 100.</summary>
    public decimal Percent { get; set; }
}
