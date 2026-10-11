using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Repositories;

public interface IItemTypeRepository
{
    Task<IReadOnlyList<ItemType>> ListAsync(CancellationToken cancellationToken);

    Task<ItemType?> FindByIdAsync(long id, CancellationToken cancellationToken);
}
