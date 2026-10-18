using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Fpfl.Items.Domain.Services;

namespace Fpfl.Items.Application.Services;

/// <summary>
///     Debt Paydown's tracked debts. A user only ever sees and changes their own (someone else's answers
///     404, as with items); the owner comes from the access token; every save goes through
///     <see cref="DebtRules" />, including the rule that a credit can't be more than 100% allocated
///     across all of the user's debts.
/// </summary>
public sealed class DebtService(IDebtRepository debts, IItemRepository items, TimeProvider clock) : IDebtService
{
    public Task<IReadOnlyList<Debt>> ListAsync(Guid requesterUserId, CancellationToken cancellationToken) =>
        debts.ListByUserAsync(requesterUserId, cancellationToken);

    public async Task<Debt> GetAsync(long id, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new DomainValidationException("Debt id must be a positive integer");
        }

        var debt = await debts.FindByIdAsync(id, cancellationToken);
        if (debt is null || (!requesterIsAdmin && debt.UserId != requesterUserId))
        {
            throw new ItemNotFoundException($"Debt {id} not found");
        }

        return debt;
    }

    public async Task<Debt> CreateAsync(DebtDraft draft, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var ownerItems = await OwnerItems(ownerUserId, cancellationToken);
        if (draft.ItemId is not { } itemId || !ownerItems.TryGetValue(itemId, out var debit))
        {
            throw new DomainValidationException("Choose one of your debits to track");
        }

        var existing = await debts.ListByUserAsync(ownerUserId, cancellationToken);
        if (existing.Any(d => d.ItemId == itemId))
        {
            throw new DuplicateItemException($"\"{debit.Name}\" is already being tracked");
        }

        var (balance, apr, balanceDate, allocations) = DebtRules.Normalize(draft, debit, ownerItems, existing, Today());
        var id = await debts.AddAsync(new Debt
        {
            UserId = ownerUserId,
            ItemId = itemId,
            Balance = balance,
            Apr = apr,
            BalanceDate = balanceDate,
            Allocations = allocations,
        }, cancellationToken);

        return await debts.FindByIdAsync(id, cancellationToken) ?? throw new ItemNotFoundException($"Debt {id} not found");
    }

    public async Task<Debt> UpdateAsync(
        long id, DebtDraft draft, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        var current = await GetAsync(id, requesterUserId, requesterIsAdmin, cancellationToken);
        var ownerItems = await OwnerItems(current.UserId, cancellationToken);
        if (!ownerItems.TryGetValue(current.ItemId, out var debit))
        {
            throw new ItemNotFoundException($"Debt {id} not found");
        }

        if (draft.ItemId is { } itemId && itemId != current.ItemId)
        {
            throw new DomainValidationException("A tracked debt's debit can't be changed; delete it and track the other debit instead");
        }

        var others = (await debts.ListByUserAsync(current.UserId, cancellationToken)).Where(d => d.Id != id);
        var (balance, apr, balanceDate, allocations) = DebtRules.Normalize(draft, debit, ownerItems, others, Today());

        await debts.UpdateAsync(new Debt
        {
            Id = id,
            UserId = current.UserId,
            ItemId = current.ItemId,
            Balance = balance,
            Apr = apr,
            BalanceDate = balanceDate,
            Allocations = allocations,
        }, cancellationToken);

        return await debts.FindByIdAsync(id, cancellationToken) ?? throw new ItemNotFoundException($"Debt {id} not found");
    }

    public async Task DeleteAsync(long id, Guid requesterUserId, bool requesterIsAdmin, CancellationToken cancellationToken)
    {
        await GetAsync(id, requesterUserId, requesterIsAdmin, cancellationToken);

        if (await debts.DeleteAsync(id, cancellationToken) == 0)
        {
            throw new ItemNotFoundException($"Debt {id} not found");
        }
    }

    private async Task<Dictionary<long, Item>> OwnerItems(Guid userId, CancellationToken cancellationToken) =>
        (await items.ListByUserAsync(userId, cancellationToken)).ToDictionary(i => i.Id);

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
