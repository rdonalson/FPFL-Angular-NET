using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Fpfl.Items.Domain.Services;

namespace Fpfl.Items.Application.Services;

/// <summary>
///     Port of API-JDK's InitialAmountController logic, moved into the service and tightened:
///     the owner comes from the access token, users can only reach their own amount, the amount
///     is validated (0 or more, like UI-TSX enforces), and only one per user is allowed.
///     Every user always has one: it is created at $0.00 at registration, or on first read.
/// </summary>
public sealed class InitialAmountService(IItemRepository repository, TimeProvider clock) : IInitialAmountService
{
    /// <summary>fpfl.item_types id for "Initial Amount".</summary>
    public const long InitialAmountType = 3;

    /// <summary>The name API-JDK stores on initial-amount items.</summary>
    public const string StoredName = "IA";

    public async Task<Item> GetOrCreateAsync(
        Guid userId, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        if (!requesterIsAdmin && userId != requesterUserId)
        {
            throw new ForbiddenOperationException("You can only view your own initial amount");
        }

        return await FindForUserAsync(userId, cancellationToken) ?? await AddAsync(userId, 0, cancellationToken);
    }

    public async Task EnsureAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await FindForUserAsync(userId, cancellationToken) is null)
        {
            await AddAsync(userId, 0, cancellationToken);
        }
    }

    public async Task<Item> CreateAsync(double? amount, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var value = ValidateAmount(amount);

        if (await FindForUserAsync(ownerUserId, cancellationToken) is not null)
        {
            throw new DuplicateItemException("You already have an initial amount; update it instead");
        }

        return await AddAsync(ownerUserId, value, cancellationToken);
    }

    public async Task<Item> UpdateAsync(
        long id, double? amount, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        var value = ValidateAmount(amount);

        var existing = id > 0 ? await repository.FindByIdAsync(id, cancellationToken) : null;
        if (existing is null
            || existing.ItemTypeId != InitialAmountType
            || (!requesterIsAdmin && existing.UserId != requesterUserId))
        {
            throw new ItemNotFoundException($"InitialAmount {id} not found");
        }

        // A fresh entity (no navigation properties) so only the items row is written.
        await repository.UpdateAsync(new Item
        {
            Id = existing.Id,
            UserId = existing.UserId,
            Name = StoredName,
            Amount = value,
            ItemTypeId = InitialAmountType,
            BeginDate = Today(), // "as of" date: when the balance was last set
        }, cancellationToken);

        return await repository.FindByIdAsync(id, cancellationToken) ?? existing;
    }

    private async Task<Item> AddAsync(Guid userId, double amount, CancellationToken cancellationToken)
    {
        var item = new Item
        {
            UserId = userId,
            Name = StoredName,
            Amount = amount,
            ItemTypeId = InitialAmountType,
            BeginDate = Today(),
        };

        var id = await repository.AddAsync(item, cancellationToken);
        return await repository.FindByIdAsync(id, cancellationToken) ?? item;
    }

    private async Task<Item?> FindForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        (await repository.ListByUserAndTypeAsync(userId, InitialAmountType, cancellationToken))
        .OrderBy(i => i.Id)
        .FirstOrDefault();

    private static double ValidateAmount(double? amount)
    {
        if (amount is not { } value || !double.IsFinite(value))
        {
            throw new DomainValidationException("InitialAmount Amount is required");
        }
        if (value < 0)
        {
            throw new DomainValidationException("InitialAmount Amount must be 0 or more");
        }
        if (value > ItemRules.MaxAmount)
        {
            throw new DomainValidationException("InitialAmount Amount is too large");
        }
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
