using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Fpfl.Items.Domain.Services;

namespace Fpfl.Items.Application.Services;

/// <summary>
///     Business rules for item types (port of API-JDK's ItemTypeServiceImpl).
///     Messages match the Java service so both APIs read the same to clients.
/// </summary>
public sealed class ItemTypeService(IItemTypeRepository repository) : IItemTypeService
{
    public const int MaxNameLength = 75;

    public Task<IReadOnlyList<ItemType>> ListAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);

    public async Task<ItemType> GetAsync(long id, CancellationToken cancellationToken)
    {
        ValidateId(id);

        return await repository.FindByIdAsync(id, cancellationToken)
               ?? throw new ItemNotFoundException($"ItemType {id} not found");
    }

    public async Task<ItemType> CreateAsync(ItemType itemType, CancellationToken cancellationToken)
    {
        ValidateId(itemType.Id);
        itemType.Name = ValidateName(itemType.Name);

        if (await repository.ExistsAsync(itemType.Id, cancellationToken))
        {
            throw new DuplicateItemException($"ItemType {itemType.Id} already exists.");
        }

        await repository.AddAsync(itemType, cancellationToken);
        return itemType;
    }

    public async Task<ItemType> UpdateAsync(long id, long? newId, string name, CancellationToken cancellationToken)
    {
        ValidateId(id);
        var targetId = newId ?? id;
        ValidateId(targetId);
        name = ValidateName(name);

        if (targetId != id && await repository.ExistsAsync(targetId, cancellationToken))
        {
            throw new DuplicateItemException($"ItemType {targetId} already exists.");
        }

        var updated = await repository.UpdateAsync(id, targetId, name, cancellationToken);
        if (updated == 0)
        {
            throw new ItemNotFoundException($"ItemType {id} not found");
        }

        return new ItemType { Id = targetId, Name = name };
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        ValidateId(id);

        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (deleted == 0)
        {
            throw new ItemNotFoundException($"ItemType {id} not found");
        }
    }

    private static void ValidateId(long id)
    {
        if (id <= 0)
        {
            throw new DomainValidationException("ItemType id must be a positive integer");
        }
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("ItemType Name is required and cannot be blank");
        }

        name = name.Trim();
        if (name.Length > MaxNameLength)
        {
            throw new DomainValidationException($"ItemType Name must be at most {MaxNameLength} characters");
        }

        return name;
    }
}
