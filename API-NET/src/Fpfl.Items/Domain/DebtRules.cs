using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain;

/// <summary>The rules a tracked debt and its allocations must satisfy, in one place.</summary>
public static class DebtRules
{
    public const decimal MaxBalance = 1_000_000_000m;
    public const decimal MaxApr = 100m;
    public const int MaxAllocations = 20;

    /// <summary>
    ///     Checks a draft against the owner's items and their other debts, and returns the values to save.
    /// </summary>
    /// <param name="draft">The request.</param>
    /// <param name="debitItem">The debit being tracked (already checked to be the owner's).</param>
    /// <param name="ownerItems">Every item the owner has, by id.</param>
    /// <param name="otherDebts">The owner's other debts (not this one), for the per-credit 100% cap.</param>
    /// <param name="today">Used when the draft has no balance date.</param>
    /// <exception cref="DomainValidationException">Any rule is broken; the message says which.</exception>
    public static (decimal Balance, decimal Apr, DateOnly BalanceDate, List<DebtAllocation> Allocations) Normalize(
        DebtDraft draft,
        Item debitItem,
        IReadOnlyDictionary<long, Item> ownerItems,
        IEnumerable<Debt> otherDebts,
        DateOnly today)
    {
        if (debitItem.ItemTypeId != ItemRules.Debit)
        {
            throw Invalid("Only a debit can be tracked as a debt");
        }

        if (draft.Balance is not { } balance || balance < 0 || balance > MaxBalance)
        {
            throw Invalid("Balance owed is required and must be between $0 and $1,000,000,000");
        }

        var apr = draft.Apr ?? 0;
        if (apr < 0 || apr > MaxApr)
        {
            throw Invalid("APR must be between 0% and 100%");
        }

        var allocations = draft.Allocations ?? Array.Empty<AllocationDraft>();
        if (allocations.Count > MaxAllocations)
        {
            throw Invalid($"A debt can have at most {MaxAllocations} allocations");
        }

        // How much of each credit the owner's other debts already take.
        var taken = otherDebts
            .SelectMany(d => d.Allocations)
            .GroupBy(a => a.CreditItemId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Percent));

        var result = new List<DebtAllocation>();
        foreach (var allocation in allocations)
        {
            if (allocation.CreditItemId is not { } creditId || !ownerItems.TryGetValue(creditId, out var credit))
            {
                throw Invalid("Each allocation needs one of your credits");
            }

            if (credit.ItemTypeId != ItemRules.Credit)
            {
                throw Invalid($"\"{credit.Name}\" is not a credit");
            }

            if (result.Any(a => a.CreditItemId == creditId))
            {
                throw Invalid($"\"{credit.Name}\" is allocated to this debt more than once");
            }

            if (allocation.Percent is not { } raw || raw <= 0 || raw > 100)
            {
                throw Invalid($"The percentage of \"{credit.Name}\" must be more than 0% and at most 100%");
            }

            var percent = decimal.Round(raw, 2, MidpointRounding.AwayFromZero);
            var already = taken.GetValueOrDefault(creditId);
            if (already + percent > 100)
            {
                throw Invalid($"\"{credit.Name}\" is already {already:0.##}% allocated to other debts; at most {100 - already:0.##}% is left");
            }

            result.Add(new DebtAllocation { CreditItemId = creditId, Percent = percent });
        }

        return (decimal.Round(balance, 2, MidpointRounding.AwayFromZero), decimal.Round(apr, 3), draft.BalanceDate ?? today, result);
    }

    private static DomainValidationException Invalid(string message) => new(message);
}
