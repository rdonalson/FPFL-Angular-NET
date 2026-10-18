using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;

namespace Fpfl.Api.Tests;

/// <summary>Tracked debts in a list, with each debt's item and allocations' credits filled in from <see cref="Items" />.</summary>
internal sealed class InMemoryDebtRepository(IEnumerable<Item>? items = null) : IDebtRepository
{
    public List<Debt> Debts { get; } = [];

    public List<Item> Items { get; } = items?.ToList() ?? [];

    public Task<IReadOnlyList<Debt>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Debt>>(Debts.Where(d => d.UserId == userId).Select(WithDetails).ToList());

    public Task<Debt?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(Debts.Where(d => d.Id == id).Select(WithDetails).FirstOrDefault());

    public Task<long> AddAsync(Debt debt, CancellationToken cancellationToken)
    {
        if (Debts.Any(d => d.ItemId == debt.ItemId))
        {
            throw new DuplicateItemException("That debit is already being tracked");
        }

        debt.Id = Debts.Count == 0 ? 1 : Debts.Max(d => d.Id) + 1;
        Debts.Add(Copy(debt));
        return Task.FromResult(debt.Id);
    }

    public Task UpdateAsync(Debt debt, CancellationToken cancellationToken)
    {
        var index = Debts.FindIndex(d => d.Id == debt.Id);
        if (index < 0)
        {
            throw new ItemNotFoundException($"Debt {debt.Id} not found");
        }

        Debts[index] = Copy(debt);
        return Task.CompletedTask;
    }

    public Task<int> DeleteAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(Debts.RemoveAll(d => d.Id == id));

    private Debt WithDetails(Debt stored)
    {
        var copy = Copy(stored);
        copy.Item = Items.FirstOrDefault(i => i.Id == copy.ItemId);
        foreach (var allocation in copy.Allocations)
        {
            allocation.CreditItem = Items.FirstOrDefault(i => i.Id == allocation.CreditItemId);
        }

        return copy;
    }

    private static Debt Copy(Debt d) => new()
    {
        Id = d.Id,
        UserId = d.UserId,
        ItemId = d.ItemId,
        Balance = d.Balance,
        Apr = d.Apr,
        BalanceDate = d.BalanceDate,
        Allocations = d.Allocations.Select((a, i) => new DebtAllocation
        {
            Id = a.Id == 0 ? i + 1 : a.Id,
            DebtId = d.Id,
            CreditItemId = a.CreditItemId,
            Percent = a.Percent,
        }).ToList(),
    };
}

/// <summary>Items in a list, for services that only read a user's items.</summary>
internal sealed class InMemoryItemRepository(IEnumerable<Item> items) : IItemRepository
{
    public List<Item> Items { get; } = items.ToList();

    public Task<IReadOnlyList<Item>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Item>>(Items.Where(i => i.UserId == userId).ToList());

    public Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Item>>(Items.ToList());

    public Task<Item?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

    public Task<IReadOnlyList<Item>> ListByUserAndTypeAsync(Guid userId, long itemTypeId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Item>>(Items.Where(i => i.UserId == userId && i.ItemTypeId == itemTypeId).ToList());

    public Task<long> AddAsync(Item item, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task UpdateAsync(Item item, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<int> DeleteAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
}
