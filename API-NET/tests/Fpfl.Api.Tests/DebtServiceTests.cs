using Fpfl.Common.Exceptions;
using Fpfl.Display.Application.Services;
using Fpfl.Display.Domain;
using Fpfl.Items.Application.Services;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Tests;

public sealed class DebtServiceTests
{
    private static readonly Guid Rick = Guid.Parse("8fdbe29e-f25f-450d-b179-92973e2bf7ba");
    private static readonly Guid Guest = Guid.Parse("1b4e28ba-2fa1-11d2-883f-0016d3cca427");
    private static readonly DateOnly Today = new(2026, 10, 18);

    private static Item Debit(long id, string name, Guid owner, double amount = 200) => new()
    {
        Id = id, UserId = owner, Name = name, Amount = amount, ItemTypeId = ItemRules.Debit, TimePeriodId = ItemRules.Monthly, MonthlyDom = 15,
    };

    private static Item Credit(long id, string name, Guid owner, double amount = 2000) => new()
    {
        Id = id, UserId = owner, Name = name, Amount = amount, ItemTypeId = ItemRules.Credit, TimePeriodId = ItemRules.Monthly, MonthlyDom = 1,
    };

    private readonly InMemoryItemRepository _items = new(
    [
        Debit(10, "Chase Visa", Rick),
        Debit(11, "Car Loan", Rick),
        Credit(20, "Paycheck", Rick),
        Credit(21, "Bonus", Rick, 1000),
        Debit(30, "Guest Card", Guest),
        Credit(40, "Guest Pay", Guest),
    ]);

    private readonly InMemoryDebtRepository _debts;
    private readonly DebtService _service;

    public DebtServiceTests()
    {
        _debts = new InMemoryDebtRepository(_items.Items);
        _service = new DebtService(_debts, _items, new FixedTimeProvider(new DateTimeOffset(2026, 10, 18, 15, 0, 0, TimeSpan.Zero)));
    }

    private static DebtDraft Draft(long? itemId, decimal? balance, params (long Credit, decimal Percent)[] allocations) =>
        new(itemId, balance, null, null, allocations.Select(a => new AllocationDraft(a.Credit, a.Percent)).ToList());

    [Fact]
    public async Task Tracks_a_debit_with_allocations_and_sensible_defaults()
    {
        var debt = await _service.CreateAsync(Draft(10, 4500.505m, (20, 33.333m), (21, 50)), Rick, default);

        Assert.Equal(Rick, debt.UserId);
        Assert.Equal(10, debt.ItemId);
        Assert.Equal("Chase Visa", debt.Item?.Name);
        Assert.Equal(4500.51m, debt.Balance);
        Assert.Equal(0m, debt.Apr);
        Assert.Equal(Today, debt.BalanceDate);
        Assert.Equal(new[] { 33.33m, 50m }, debt.Allocations.Select(a => a.Percent));
        Assert.Equal("Paycheck", debt.Allocations[0].CreditItem?.Name);
    }

    [Theory]
    [InlineData(20L, "Only a debit")] // a credit can't be a debt
    [InlineData(30L, "Choose one of your debits")] // someone else's debit
    [InlineData(null, "Choose one of your debits")]
    public async Task Only_your_own_debits_can_be_tracked(long? itemId, string message)
    {
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(Draft(itemId, 100), Rick, default));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public async Task A_debit_is_tracked_only_once()
    {
        await _service.CreateAsync(Draft(10, 100), Rick, default);

        await Assert.ThrowsAsync<DuplicateItemException>(() => _service.CreateAsync(Draft(10, 200), Rick, default));
    }

    [Fact]
    public async Task Balance_and_apr_must_be_in_range()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(Draft(10, -1), Rick, default));
        await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(Draft(10, null), Rick, default));
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.CreateAsync(new DebtDraft(10, 100, 101, null, []), Rick, default));
    }

    [Theory]
    [InlineData(40L, 10, "one of your credits")] // someone else's credit
    [InlineData(11L, 10, "is not a credit")] // a debit
    [InlineData(20L, 0, "more than 0%")]
    [InlineData(20L, 100.5, "at most 100%")]
    public async Task Allocations_must_be_your_credits_with_a_sensible_percentage(long creditId, double percent, string message)
    {
        var ex = await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.CreateAsync(Draft(10, 100, (creditId, (decimal)percent)), Rick, default));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public async Task A_credit_appears_once_per_debt()
    {
        var ex = await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.CreateAsync(Draft(10, 100, (20, 10), (20, 20)), Rick, default));
        Assert.Contains("more than once", ex.Message);
    }

    [Fact]
    public async Task A_credit_cant_be_more_than_100_percent_allocated_across_debts()
    {
        await _service.CreateAsync(Draft(10, 100, (20, 70)), Rick, default);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.CreateAsync(Draft(11, 100, (20, 40)), Rick, default));
        Assert.Contains("already 70% allocated", ex.Message);
        Assert.Contains("at most 30% is left", ex.Message);

        var loan = await _service.CreateAsync(Draft(11, 100, (20, 30)), Rick, default);
        Assert.Equal(30m, loan.Allocations[0].Percent);
    }

    [Fact]
    public async Task Update_changes_the_balance_and_allocations_and_ignores_its_own_old_share()
    {
        var card = await _service.CreateAsync(Draft(10, 1000, (20, 70)), Rick, default);

        // 90% is fine: the 70% being replaced is this debt's own.
        var updated = await _service.UpdateAsync(
            card.Id, new DebtDraft(null, 800, 19.99m, new DateOnly(2026, 11, 1), [new(20, 90), new(21, 10)]), Rick, false, default);

        Assert.Equal(800m, updated.Balance);
        Assert.Equal(19.99m, updated.Apr);
        Assert.Equal(new DateOnly(2026, 11, 1), updated.BalanceDate);
        Assert.Equal(new[] { 90m, 10m }, updated.Allocations.Select(a => a.Percent));
    }

    [Fact]
    public async Task The_debit_of_a_tracked_debt_cant_change()
    {
        var card = await _service.CreateAsync(Draft(10, 1000), Rick, default);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.UpdateAsync(card.Id, Draft(11, 1000), Rick, false, default));
    }

    [Fact]
    public async Task Someone_elses_debt_answers_not_found_unless_admin()
    {
        var card = await _service.CreateAsync(Draft(10, 1000), Rick, default);

        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.GetAsync(card.Id, Guest, false, default));
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.UpdateAsync(card.Id, Draft(null, 1), Guest, false, default));
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.DeleteAsync(card.Id, Guest, false, default));
        Assert.Empty(await _service.ListAsync(Guest, default));

        Assert.Equal(card.Id, (await _service.GetAsync(card.Id, Guest, true, default)).Id);
    }

    [Fact]
    public async Task Delete_stops_tracking()
    {
        var card = await _service.CreateAsync(Draft(10, 1000), Rick, default);

        await _service.DeleteAsync(card.Id, Rick, false, default);

        Assert.Empty(await _service.ListAsync(Rick, default));
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.DeleteAsync(card.Id, Rick, false, default));
    }

    [Fact]
    public async Task The_ledger_deducts_allocations_and_the_paydown_follows_the_debt()
    {
        // Paycheck $2,000 on the 1st; 25% goes to the Chase Visa ($1,000 owed as of Oct 18).
        await _service.CreateAsync(Draft(10, 1000, (20, 25)), Rick, default);
        var readout = new LedgerReadoutService(_items, _debts);

        var days = await readout.BuildAsync(Rick, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1), Rick, false, default);
        var paydownEntry = Assert.Single(days[0].Entries, e => e.ItemTypeName == LedgerBuilder.PaydownTypeName);
        Assert.Equal(-500m, paydownEntry.Amount);
        Assert.Equal(2500m, days[0].RunningTotal); // paycheck 2,000 + bonus 1,000 in, 500 to the card

        var debt = Assert.Single(await readout.BuildPaydownAsync(Rick, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1), Rick, false, default));
        Assert.Equal(500m, debt.EndingBalance);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
