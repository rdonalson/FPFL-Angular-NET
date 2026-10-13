using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Services;

public interface IItemTypeService
{
    Task<IReadOnlyList<ItemType>> ListAsync(CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">id is not positive.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No item type with that id.</exception>
    Task<ItemType> GetAsync(long id, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DuplicateItemException">The id already exists.</exception>
    Task<ItemType> CreateAsync(ItemType itemType, CancellationToken cancellationToken);

    /// <summary>Renames the item type and, when <paramref name="newId" /> is given and different, changes its id.</summary>
    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No item type with that id.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DuplicateItemException">The new id already exists.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">The id changes but items still use it.</exception>
    Task<ItemType> UpdateAsync(long id, long? newId, string name, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No item type with that id.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">Items still use this item type.</exception>
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
