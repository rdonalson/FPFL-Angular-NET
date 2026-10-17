using Fpfl.Common.Exceptions;
using Fpfl.Items.Application.Services;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;

namespace Fpfl.Api.Tests;

public sealed class InitialAmountServiceTests
{
    private static readonly Guid Rick = Guid.Parse("8fdbe29e-f25f-450d-b179-92973e2bf7ba");
    private static readonly Guid Guest = Guid.Parse("1b4e28ba-2fa1-11d2-883f-0016d3cca427");
    private static readonly DateOnly Today = new(2026, 10, 17);

    private readonly FakeItemRepository _repo = new();
    private readonly InitialAmountService _service;

    public InitialAmountServiceTests()
    {
        _service = new InitialAmountService(_repo, new FixedTimeProvider(new DateTimeOffset(2026, 10, 17, 15, 0, 0, TimeSpan.Zero)));
        _repo.Items.Add(new Item { Id = 1, UserId = Rick, Name = "IA", Amount = 1200, ItemTypeId = 3, BeginDate = new DateOnly(2026, 1, 1) });
        _repo.Items.Add(new Item { Id = 2, UserId = Rick, Name = "Salary", Amount = 4000, ItemTypeId = 1, TimePeriodId = 6 });
    }

    [Fact]
    public async Task Returns_the_users_existing_initial_amount()
    {
        var item = await _service.GetOrCreateAsync(Rick, Rick, requesterIsAdmin: false, default);

        Assert.Equal(1, item.Id);
        Assert.Equal(1200, item.Amount);
        Assert.Equal(2, _repo.Items.Count);
    }

    [Fact]
    public async Task Creates_a_zero_amount_on_first_read()
    {
        var item = await _service.GetOrCreateAsync(Guest, Guest, requesterIsAdmin: false, default);

        Assert.Equal(Guest, item.UserId);
        Assert.Equal(0, item.Amount);
        Assert.Equal("IA", item.Name);
        Assert.Equal(3, item.ItemTypeId);
        Assert.Equal(Today, item.BeginDate);

        await _service.GetOrCreateAsync(Guest, Guest, requesterIsAdmin: false, default);
        Assert.Single(_repo.Items, i => i.UserId == Guest); // only ever one
    }

    [Fact]
    public async Task Another_users_amount_is_forbidden_unless_admin()
    {
        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => _service.GetOrCreateAsync(Rick, Guest, requesterIsAdmin: false, default));

        var item = await _service.GetOrCreateAsync(Rick, Guest, requesterIsAdmin: true, default);
        Assert.Equal(1200, item.Amount);
    }

    [Fact]
    public async Task Ensure_creates_one_only_when_missing()
    {
        await _service.EnsureAsync(Guest, default);
        await _service.EnsureAsync(Guest, default);
        await _service.EnsureAsync(Rick, default);

        Assert.Single(_repo.Items, i => i.UserId == Guest && i.ItemTypeId == 3);
        Assert.Single(_repo.Items, i => i.UserId == Rick && i.ItemTypeId == 3);
    }

    [Fact]
    public async Task Create_rejects_a_second_initial_amount()
    {
        await Assert.ThrowsAsync<DuplicateItemException>(() => _service.CreateAsync(10, Rick, default));

        var created = await _service.CreateAsync(99.999, Guest, default);
        Assert.Equal(100.00, created.Amount);
    }

    [Fact]
    public async Task Update_sets_the_amount_and_the_as_of_date()
    {
        var item = await _service.UpdateAsync(1, 2500, Rick, requesterIsAdmin: false, default);

        Assert.Equal(2500, item.Amount);
        Assert.Equal(Today, item.BeginDate);
        Assert.Equal(Rick, item.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-0.01)]
    [InlineData(double.PositiveInfinity)]
    public async Task Amount_must_be_zero_or_more(double? amount)
    {
        await Assert.ThrowsAsync<DomainValidationException>(() => _service.UpdateAsync(1, amount, Rick, requesterIsAdmin: false, default));
    }

    [Fact]
    public async Task Zero_is_a_valid_amount()
    {
        Assert.Equal(0, (await _service.UpdateAsync(1, 0, Rick, requesterIsAdmin: false, default)).Amount);
    }

    [Fact]
    public async Task Update_only_reaches_the_callers_own_initial_amount()
    {
        // Someone else's, and an item that isn't an initial amount at all.
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.UpdateAsync(1, 5, Guest, requesterIsAdmin: false, default));
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.UpdateAsync(2, 5, Rick, requesterIsAdmin: false, default));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeItemRepository : IItemRepository
    {
        public List<Item> Items { get; } = [];

        public Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Item>>(Items.ToList());

        public Task<Item?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<IReadOnlyList<Item>> ListByUserAndTypeAsync(Guid userId, long itemTypeId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Item>>(Items.Where(i => i.UserId == userId && i.ItemTypeId == itemTypeId).ToList());

        public Task<long> AddAsync(Item item, CancellationToken cancellationToken)
        {
            item.Id = Items.Max(i => i.Id) + 1;
            Items.Add(item);
            return Task.FromResult(item.Id);
        }

        public Task UpdateAsync(Item item, CancellationToken cancellationToken)
        {
            Items.RemoveAll(i => i.Id == item.Id);
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task<int> DeleteAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.RemoveAll(i => i.Id == id));
    }
}
