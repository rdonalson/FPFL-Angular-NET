using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Services;

/// <summary>A user's tracked debts and the credits allocated to them (Debt Paydown).</summary>
public interface IDebtService
{
    Task<IReadOnlyList<Debt>> ListAsync(Guid requesterUserId, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">Missing, or someone else's (unless admin).</exception>
    Task<Debt> GetAsync(long id, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken);

    /// <summary>Starts tracking one of the owner's debits.</summary>
    Task<Debt> CreateAsync(DebtDraft draft, Guid ownerUserId, CancellationToken cancellationToken);

    /// <summary>Changes the balance, APR, balance date and allocations; the debit itself can't change.</summary>
    Task<Debt> UpdateAsync(long id, DebtDraft draft, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken);

    /// <summary>Stops tracking the debt. The debit item itself is untouched.</summary>
    Task DeleteAsync(long id, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken);
}
