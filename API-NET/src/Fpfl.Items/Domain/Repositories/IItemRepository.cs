using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Repositories;

/// <summary>Read access to items; each result includes its item type and time period.</summary>
public interface IItemRepository
{
    Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken);

    Task<Item?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Item>> ListByUserAndTypeAsync(Guid userId, long itemTypeId, CancellationToken cancellationToken);

    /// <summary>Every item the user owns (credits, debits and the initial amount), for the ledger.</summary>
    Task<IReadOnlyList<Item>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <returns>The new item's id.</returns>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">The item type or time period does not exist.</exception>
    Task<long> AddAsync(Item item, CancellationToken cancellationToken);

    /// <summary>Overwrites every column of the row with <paramref name="item" />'s values (matched by Id).</summary>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">The item type or time period does not exist.</exception>
    Task UpdateAsync(Item item, CancellationToken cancellationToken);

    /// <returns>Rows deleted (0 or 1).</returns>
    Task<int> DeleteAsync(long id, CancellationToken cancellationToken);
}
