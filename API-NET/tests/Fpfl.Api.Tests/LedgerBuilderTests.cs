using Fpfl.Common.Exceptions;
using Fpfl.Display.Application.Services;
using Fpfl.Display.Domain;
using Fpfl.Display.Domain.Recurrence;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;

namespace Fpfl.Api.Tests;

public sealed class LedgerBuilderTests
{
    private static readonly Guid Rick = Guid.Parse("8fdbe29e-f25f-450d-b179-92973e2bf7ba");
    private static readonly Guid Guest = Guid.Parse("1b4e28ba-2fa1-11d2-883f-0016d3cca427");

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static Item OneTime(long id, long type, string name, double amount, DateOnly date) => new()
    {
        Id = id,
        UserId = Rick,
        ItemTypeId = type,
        ItemType = new ItemType { Id = type, Name = type == ItemRules.Credit ? "Credit" : "Debit" },
        TimePeriodId = ItemRules.OneTime,
        TimePeriod = new TimePeriod { Id = ItemRules.OneTime, Name = "One Time" },
        Name = name,
        Amount = amount,
        BeginDate = date,
        DateRangeReq = true,
    };

    private static Item Daily(long id, long type, string name, double amount) => new()
    {
        Id = id,
        UserId = Rick,
        ItemTypeId = type,
        TimePeriodId = ItemRules.Daily,
        Name = name,
        Amount = amount,
    };

    private static Item InitialAmount(double amount) => new() { Id = 99, UserId = Rick, ItemTypeId = 3, Name = "IA", Amount = amount };

    [Fact]
    public void Builds_one_row_per_day_with_a_running_balance()
    {
        Item[] items =
        [
            InitialAmount(1000),
            OneTime(1, ItemRules.Credit, "Paycheck", 500, D(10, 15)),
            OneTime(2, ItemRules.Debit, "Rent", 200, D(10, 15)),
            Daily(3, ItemRules.Debit, "Coffee", 10),
        ];

        var days = LedgerBuilder.Build(items, new DateWindow(D(10, 14), D(10, 16)));

        Assert.Equal(new[] { 1, 2, 3 }, days.Select(d => d.RollupKey));
        Assert.Equal(new[] { D(10, 14), D(10, 15), D(10, 16) }, days.Select(d => d.Date));

        Assert.Equal(-10m, days[0].Net);
        Assert.Equal(990m, days[0].RunningTotal);

        Assert.Equal(500m, days[1].CreditSummary);
        Assert.Equal(-210m, days[1].DebitSummary);
        Assert.Equal(290m, days[1].Net);
        Assert.Equal(1280m, days[1].RunningTotal);

        Assert.Equal(1270m, days[2].RunningTotal);
    }

    [Fact]
    public void Lists_a_days_credits_first_with_signed_amounts_and_names()
    {
        Item[] items =
        [
            OneTime(2, ItemRules.Debit, "Rent", 200, D(10, 15)),
            Daily(3, ItemRules.Debit, "Coffee", 10),
            OneTime(1, ItemRules.Credit, "Paycheck", 500, D(10, 15)),
        ];

        var entries = LedgerBuilder.Build(items, new DateWindow(D(10, 15), D(10, 15)))[0].Entries;

        Assert.Equal(new[] { "Paycheck", "Coffee", "Rent" }, entries.Select(e => e.Name));
        Assert.Equal(new[] { 1, 2, 3 }, entries.Select(e => e.ItemKey));
        Assert.Equal(new[] { 500m, -10m, -200m }, entries.Select(e => e.Amount));
        Assert.Equal("Credit", entries[0].ItemTypeName);
        Assert.Equal("One Time", entries[0].PeriodName);
        Assert.Equal(1, entries[0].ItemId);
    }

    [Fact]
    public void Starts_from_zero_without_an_initial_amount_and_never_lists_it()
    {
        var days = LedgerBuilder.Build([Daily(3, ItemRules.Credit, "Tips", 5)], new DateWindow(D(10, 1), D(10, 2)));
        Assert.Equal(10m, days[1].RunningTotal);

        var withIa = LedgerBuilder.Build([InitialAmount(50)], new DateWindow(D(10, 1), D(10, 2)));
        Assert.All(withIa, d => Assert.Empty(d.Entries));
        Assert.All(withIa, d => Assert.Equal(50m, d.RunningTotal));
    }

    [Fact]
    public void Sums_cents_exactly()
    {
        var days = LedgerBuilder.Build([Daily(1, ItemRules.Credit, "Dime", 0.1)], new DateWindow(D(10, 1), D(10, 3)));

        Assert.Equal(0.3m, days[2].RunningTotal); // a double sum would be 0.30000000000000004
    }

    [Fact]
    public void Ignores_items_that_are_neither_credits_nor_debits()
    {
        var odd = Daily(5, 42, "Unknown type", 100);

        var days = LedgerBuilder.Build([odd], new DateWindow(D(10, 1), D(10, 1)));

        Assert.Empty(days[0].Entries);
        Assert.Equal(0m, days[0].RunningTotal);
    }

    [Fact]
    public async Task The_service_only_builds_the_callers_own_ledger_unless_admin()
    {
        var service = new LedgerReadoutService(new FakeItemRepository([InitialAmount(250)]), new InMemoryDebtRepository());

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => service.BuildAsync(Rick, D(10, 1), D(10, 2), Guest, requesterIsAdmin: false, default));

        var days = await service.BuildAsync(Rick, D(10, 1), D(10, 2), Guest, requesterIsAdmin: true, default);
        Assert.Equal(250m, days[^1].RunningTotal);
    }

    [Fact]
    public async Task The_service_rejects_backwards_or_overlong_ranges()
    {
        var service = new LedgerReadoutService(new FakeItemRepository([]), new InMemoryDebtRepository());

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.BuildAsync(Rick, D(10, 2), D(10, 1), Rick, requesterIsAdmin: false, default));
        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.BuildAsync(Rick, D(1, 1), new DateOnly(2036, 1, 2), Rick, requesterIsAdmin: false, default));

        var single = await service.BuildAsync(Rick, D(10, 1), D(10, 1), Rick, requesterIsAdmin: false, default);
        Assert.Single(single);
    }

    private sealed class FakeItemRepository(IEnumerable<Item> items) : IItemRepository
    {
        private readonly List<Item> _items = items.ToList();

        public Task<IReadOnlyList<Item>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Item>>(_items.Where(i => i.UserId == userId).ToList());

        public Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Item?> FindByIdAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Item>> ListByUserAndTypeAsync(Guid userId, long itemTypeId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<long> AddAsync(Item item, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task UpdateAsync(Item item, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> DeleteAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
