using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Repositories;

/// <summary>Read access to items; each result includes its item type and time period.</summary>
public interface IItemRepository
{
    Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken);

    Task<Item?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Item>> ListByUserAndTypeAsync(Guid userId, long itemTypeId, CancellationToken cancellationToken);
}
