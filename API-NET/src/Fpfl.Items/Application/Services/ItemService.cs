using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Fpfl.Items.Domain.Services;

namespace Fpfl.Items.Application.Services;

/// <summary>
///     Port of API-JDK's ItemServiceImpl, plus what it lacks: a user can only read or change their own
///     items (admins can reach all), the owner always comes from the access token, and every save goes
///     through <see cref="ItemRules" />.
/// </summary>
public sealed class ItemService(IItemRepository repository) : IItemService
{
    public Task<IReadOnlyList<Item>> ListAllAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);

    public async Task<Item> GetAsync(long id, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new DomainValidationException("Item id must be a positive integer");
        }

        var item = await repository.FindByIdAsync(id, cancellationToken);

        // Someone else's item answers exactly like a missing one, so ids can't be probed.
        if (item is null || (!requesterIsAdmin && item.UserId != requesterUserId))
        {
            throw new ItemNotFoundException($"Item {id} not found");
        }

        return item;
    }

    public async Task<Item> CreateAsync(Item draft, Guid ownerUserId, CancellationToken cancellationToken)
    {
        draft.Id = 0;
        draft.UserId = ownerUserId;
        var item = ItemRules.Normalize(draft);

        var id = await repository.AddAsync(item, cancellationToken);

        // Re-read so the response carries the item type and time period names.
        return await repository.FindByIdAsync(id, cancellationToken) ?? item;
    }

    public async Task<Item> UpdateAsync(
        long id, Item draft, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        var existing = await GetAsync(id, requesterUserId, requesterIsAdmin, cancellationToken);

        draft.Id = existing.Id;
        draft.UserId = existing.UserId; // an update never moves an item to another user
        var item = ItemRules.Normalize(draft);

        await repository.UpdateAsync(item, cancellationToken);
        return await repository.FindByIdAsync(id, cancellationToken) ?? item;
    }

    public async Task DeleteAsync(long id, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        await GetAsync(id, requesterUserId, requesterIsAdmin, cancellationToken);

        if (await repository.DeleteAsync(id, cancellationToken) == 0)
        {
            throw new ItemNotFoundException($"Item {id} not found");
        }
    }

    public Task<IReadOnlyList<Item>> ListForUserAsync(
        Guid userId, long itemTypeId, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        if (itemTypeId <= 0)
        {
            throw new DomainValidationException("ItemType id must be a positive integer");
        }

        if (!requesterIsAdmin && userId != requesterUserId)
        {
            throw new ForbiddenOperationException("You can only view your own items");
        }

        return repository.ListByUserAndTypeAsync(userId, itemTypeId, cancellationToken);
    }
}
