using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Repositories;

/// <summary>Tracked debts; each result includes its debit item and its allocations (with their credit items).</summary>
public interface IDebtRepository
{
    Task<IReadOnlyList<Debt>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Debt?> FindByIdAsync(long id, CancellationToken cancellationToken);

    /// <returns>The new debt's id.</returns>
    /// <exception cref="Fpfl.Common.Exceptions.DuplicateItemException">The debit is already tracked.</exception>
    Task<long> AddAsync(Debt debt, CancellationToken cancellationToken);

    /// <summary>Saves the balance, APR, balance date and allocations (added, changed and removed) of an existing debt.</summary>
    Task UpdateAsync(Debt debt, CancellationToken cancellationToken);

    /// <returns>Rows deleted (0 or 1). Its allocations go with it.</returns>
    Task<int> DeleteAsync(long id, CancellationToken cancellationToken);
}
