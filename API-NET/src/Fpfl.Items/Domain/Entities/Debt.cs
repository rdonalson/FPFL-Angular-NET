namespace Fpfl.Items.Domain.Entities;

/// <summary>
///     A debit the user is paying down (for example a credit card's monthly payment), with what is owed
///     on it. Maps to <c>fpfl.debts</c> (created by <c>db/001_debt_paydown.sql</c>; not part of API-JDK).
/// </summary>
public sealed class Debt
{
    public long Id { get; set; }

    /// <summary>Owner: <c>users.user_id</c>, always the same as the debit item's owner.</summary>
    public Guid UserId { get; set; }

    /// <summary>The debit whose scheduled payments pay this debt down. One debt per debit.</summary>
    public long ItemId { get; set; }

    public Item? Item { get; set; }

    /// <summary>Amount owed at the end of <see cref="BalanceDate" />.</summary>
    public decimal Balance { get; set; }

    /// <summary>Annual percentage rate, e.g. 24.99; 0 for none. Charged monthly as APR / 12.</summary>
    public decimal Apr { get; set; }

    /// <summary>The day <see cref="Balance" /> was true; payments after it reduce the balance.</summary>
    public DateOnly BalanceDate { get; set; }

    public List<DebtAllocation> Allocations { get; set; } = [];
}
