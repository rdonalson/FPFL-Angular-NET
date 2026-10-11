using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Fpfl.Items.Domain.Services;

namespace Fpfl.Items.Application.Services;

/// <summary>
///     Business rules for item types. Validates input and turns "not found" into a domain exception;
///     the API layer never sees nulls.
/// </summary>
public sealed class ItemTypeService(IItemTypeRepository repository) : IItemTypeService
{
    public Task<IReadOnlyList<ItemType>> ListAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);

    public async Task<ItemType> GetAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new DomainValidationException("ItemType id must be a positive integer");
        }

        return await repository.FindByIdAsync(id, cancellationToken)
               ?? throw new ItemNotFoundException($"ItemType {id} not found");
    }
}
