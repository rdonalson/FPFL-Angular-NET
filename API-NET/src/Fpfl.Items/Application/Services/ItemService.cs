using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Fpfl.Items.Domain.Services;

namespace Fpfl.Items.Application.Services;

/// <summary>
///     Read side of API-JDK's ItemServiceImpl, plus ownership checks API-JDK doesn't have:
///     a user can only read their own items unless they are an admin.
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
