using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Services;

public interface IItemTypeService
{
    Task<IReadOnlyList<ItemType>> ListAsync(CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">id is not positive.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No item type with that id.</exception>
    Task<ItemType> GetAsync(long id, CancellationToken cancellationToken);
}
