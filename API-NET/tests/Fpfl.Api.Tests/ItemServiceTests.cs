using Fpfl.Common.Exceptions;
using Fpfl.Items.Application.Services;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;

namespace Fpfl.Api.Tests;

public sealed class ItemServiceTests
{
    private const long Credit = 1;
    private const long Debit = 2;

    private static readonly Guid Rick = Guid.Parse("8fdbe29e-f25f-450d-b179-92973e2bf7ba");
    private static readonly Guid Guest = Guid.Parse("1b4e28ba-2fa1-11d2-883f-0016d3cca427");

    private readonly FakeItemRepository _repo = new();
    private readonly ItemService _service;

    public ItemServiceTests()
    {
        _service = new ItemService(_repo);
        _repo.Items.AddRange(
        [
            new Item { Id = 1, UserId = Rick, Name = "Salary", Amount = 4000, ItemTypeId = Credit },
            new Item { Id = 2, UserId = Rick, Name = "Rent", Amount = 1500, ItemTypeId = Debit },
            new Item { Id = 3, UserId = Rick, Name = "Bonus", Amount = 500, ItemTypeId = Credit },
            new Item { Id = 4, UserId = Guest, Name = "Allowance", Amount = 50, ItemTypeId = Credit },
        ]);
    }

    [Fact]
    public async Task Users_get_only_their_own_items_of_the_requested_type()
    {
        var credits = await _service.ListForUserAsync(Rick, Credit, Rick, requesterIsAdmin: false, default);

        Assert.Equal(new[] { "Bonus", "Salary" }, credits.Select(i => i.Name).ToArray());
    }

    [Fact]
    public async Task Asking_for_another_users_items_is_forbidden()
    {
        var error = await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => _service.ListForUserAsync(Rick, Credit, Guest, requesterIsAdmin: false, default));

        Assert.Equal("You can only view your own items", error.Message);
    }

    [Fact]
    public async Task Admins_can_list_any_users_items()
    {
        var debits = await _service.ListForUserAsync(Rick, Debit, Guest, requesterIsAdmin: true, default);

        Assert.Equal("Rent", Assert.Single(debits).Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Non_positive_item_types_are_a_validation_error(long itemType)
    {
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.ListForUserAsync(Rick, itemType, Rick, requesterIsAdmin: false, default));
    }

    [Fact]
    public async Task Get_returns_the_callers_own_item()
    {
        var item = await _service.GetAsync(2, Rick, requesterIsAdmin: false, default);

        Assert.Equal("Rent", item.Name);
    }

    [Fact]
    public async Task Someone_elses_item_looks_exactly_like_a_missing_one()
    {
        var notYours = await Assert.ThrowsAsync<ItemNotFoundException>(
            () => _service.GetAsync(4, Rick, requesterIsAdmin: false, default));
        var missing = await Assert.ThrowsAsync<ItemNotFoundException>(
            () => _service.GetAsync(99, Rick, requesterIsAdmin: false, default));

        Assert.Equal("Item 4 not found", notYours.Message);
        Assert.Equal("Item 99 not found", missing.Message);
    }

    [Fact]
    public async Task Admins_can_get_any_item()
    {
        var item = await _service.GetAsync(4, Rick, requesterIsAdmin: true, default);

        Assert.Equal(Guest, item.UserId);
    }

    [Fact]
    public async Task Non_positive_ids_are_a_validation_error()
    {
        var error = await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.GetAsync(0, Rick, requesterIsAdmin: false, default));

        Assert.Equal("Item id must be a positive integer", error.Message);
    }

    [Fact]
    public async Task List_all_returns_every_item()
    {
        Assert.Equal(4, (await _service.ListAllAsync(default)).Count);
    }

    private static Item MonthlyDraft(string name = "Side Gig") => new()
    {
        Name = name, Amount = 250, ItemTypeId = Credit, TimePeriodId = 6, MonthlyDom = 15,
    };

    [Fact]
    public async Task Create_takes_the_owner_from_the_caller_and_validates()
    {
        var draft = MonthlyDraft();
        draft.UserId = Guest; // whatever the request said is ignored

        var created = await _service.CreateAsync(draft, Rick, default);

        Assert.Equal(Rick, created.UserId);
        Assert.Equal(5, created.Id);
        Assert.Equal(15, created.MonthlyDom);
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.CreateAsync(new Item { Name = "", Amount = 1, ItemTypeId = Credit, TimePeriodId = 2 }, Rick, default));
    }

    [Fact]
    public async Task Update_replaces_the_details_but_never_the_owner()
    {
        var draft = MonthlyDraft("Rent (new)");
        draft.UserId = Guest;

        var updated = await _service.UpdateAsync(2, draft, Rick, requesterIsAdmin: false, default);

        Assert.Equal("Rent (new)", updated.Name);
        Assert.Equal(Rick, updated.UserId);
        Assert.Equal(Rick, _repo.Items.Single(i => i.Id == 2).UserId);
    }

    [Fact]
    public async Task Users_cannot_update_or_delete_someone_elses_item()
    {
        await Assert.ThrowsAsync<ItemNotFoundException>(
            () => _service.UpdateAsync(4, MonthlyDraft(), Rick, requesterIsAdmin: false, default));
        await Assert.ThrowsAsync<ItemNotFoundException>(
            () => _service.DeleteAsync(4, Rick, requesterIsAdmin: false, default));

        Assert.Contains(_repo.Items, i => i.Id == 4);
    }

    [Fact]
    public async Task Delete_removes_the_item_and_a_second_delete_is_not_found()
    {
        await _service.DeleteAsync(2, Rick, requesterIsAdmin: false, default);

        Assert.DoesNotContain(_repo.Items, i => i.Id == 2);
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.DeleteAsync(2, Rick, requesterIsAdmin: false, default));
    }

    private sealed class FakeItemRepository : IItemRepository
    {
        public List<Item> Items { get; } = [];

        public Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Item>>(Items.OrderBy(i => i.Name).ToList());

        public Task<Item?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<IReadOnlyList<Item>> ListByUserAndTypeAsync(
            Guid userId, long itemTypeId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Item>>(Items
                .Where(i => i.UserId == userId && i.ItemTypeId == itemTypeId)
                .OrderBy(i => i.Name)
                .ToList());

        public Task<IReadOnlyList<Item>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Item>>(Items.Where(i => i.UserId == userId).ToList());

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
