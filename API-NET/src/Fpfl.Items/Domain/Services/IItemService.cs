using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Services;

/// <summary>
///     Reads items. Callers pass who is asking so the service, not the controller, enforces
///     that users only ever see their own items (admins can see everyone's).
/// </summary>
public interface IItemService
{
    /// <summary>Every item, for admins.</summary>
    Task<IReadOnlyList<Item>> ListAllAsync(CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">id is not positive.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No such item, or it belongs to someone else.</exception>
    Task<Item> GetAsync(long id, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken);

    /// <summary>A user's items of one type (1 = credit, 2 = debit), ordered by name.</summary>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">itemTypeId is not positive.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.ForbiddenOperationException">Asking for another user's items without being an admin.</exception>
    Task<IReadOnlyList<Item>> ListForUserAsync(
        Guid userId, long itemTypeId, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken);
}
