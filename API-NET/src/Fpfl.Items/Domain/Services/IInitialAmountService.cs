using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Services;

/// <summary>
///     A user's starting balance for the ledger. Stored like API-JDK: one item of type 3 named "IA",
///     with the date it was last set as its begin date.
/// </summary>
public interface IInitialAmountService
{
    /// <summary>
    ///     The user's initial amount, created at $0.00 first if they don't have one yet (users from
    ///     before auto-creation, or if creating it at registration failed).
    /// </summary>
    /// <exception cref="Fpfl.Common.Exceptions.ForbiddenOperationException">Another user's amount, and the caller isn't an admin.</exception>
    Task<Item> GetOrCreateAsync(Guid userId, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken);

    /// <summary>Makes sure the user has an initial amount, creating one at $0.00 if not. Called after registration.</summary>
    Task EnsureAsync(Guid userId, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">The amount is missing, negative or too large.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DuplicateItemException">The user already has one (update it instead).</exception>
    Task<Item> CreateAsync(double? amount, Guid ownerUserId, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">The amount is missing, negative or too large.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No such initial amount, or it belongs to someone else.</exception>
    Task<Item> UpdateAsync(long id, double? amount, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken);
}
