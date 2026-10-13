using Fpfl.Common.Exceptions;
using Fpfl.Items.Application.Services;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;

namespace Fpfl.Api.Tests;

public sealed class TimePeriodServiceTests
{
    private readonly FakeTimePeriodRepository _repo = new();
    private readonly TimePeriodService _service;

    public TimePeriodServiceTests()
    {
        _service = new TimePeriodService(_repo);
        _repo.Rows[1] = "One Time";
        _repo.Rows[2] = "Daily";
    }

    [Fact]
    public async Task List_returns_rows_in_id_order()
    {
        var list = await _service.ListAsync(default);

        Assert.Equal(new long[] { 1, 2 }, list.Select(t => t.Id).ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Non_positive_ids_are_a_validation_error(long id)
    {
        var error = await Assert.ThrowsAsync<DomainValidationException>(() => _service.GetAsync(id, default));
        Assert.Equal("TimePeriod id must be a positive integer", error.Message);
    }

    [Fact]
    public async Task Missing_id_is_not_found()
    {
        var error = await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.GetAsync(42, default));
        Assert.Equal("TimePeriod 42 not found", error.Message);
    }

    [Fact]
    public async Task Create_trims_the_name_and_stores_it()
    {
        var created = await _service.CreateAsync(new TimePeriod { Id = 3, Name = "  Weekly " }, default);

        Assert.Equal("Weekly", created.Name);
        Assert.Equal("Weekly", _repo.Rows[3]);
    }

    [Fact]
    public async Task Create_with_existing_id_is_a_duplicate()
    {
        var error = await Assert.ThrowsAsync<DuplicateItemException>(
            () => _service.CreateAsync(new TimePeriod { Id = 1, Name = "Again" }, default));
        Assert.Equal("TimePeriod 1 already exists.", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_names_are_rejected(string name)
    {
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.CreateAsync(new TimePeriod { Id = 3, Name = name }, default));
    }

    [Fact]
    public async Task Names_over_75_characters_are_rejected()
    {
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.UpdateAsync(1, null, new string('a', 76), default));
    }

    [Fact]
    public async Task Update_without_an_id_only_renames()
    {
        var updated = await _service.UpdateAsync(2, null, "Every Day", default);

        Assert.Equal(2, updated.Id);
        Assert.Equal("Every Day", _repo.Rows[2]);
    }

    [Fact]
    public async Task Update_with_a_new_id_moves_the_row()
    {
        var updated = await _service.UpdateAsync(2, 5, "Every Day", default);

        Assert.Equal(5, updated.Id);
        Assert.False(_repo.Rows.ContainsKey(2));
        Assert.Equal("Every Day", _repo.Rows[5]);
    }

    [Fact]
    public async Task Update_to_an_existing_id_is_a_duplicate()
    {
        var error = await Assert.ThrowsAsync<DuplicateItemException>(() => _service.UpdateAsync(2, 1, "X", default));

        Assert.Equal("TimePeriod 1 already exists.", error.Message);
        Assert.True(_repo.Rows.ContainsKey(2));
    }

    [Fact]
    public async Task Update_to_a_non_positive_id_is_a_validation_error()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() => _service.UpdateAsync(2, 0, "X", default));
    }

    [Fact]
    public async Task Update_of_missing_id_is_not_found()
    {
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.UpdateAsync(9, null, "X", default));
    }

    [Fact]
    public async Task Delete_removes_the_row_and_a_second_delete_is_not_found()
    {
        await _service.DeleteAsync(2, default);

        Assert.False(_repo.Rows.ContainsKey(2));
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.DeleteAsync(2, default));
    }

    private sealed class FakeTimePeriodRepository : ITimePeriodRepository
    {
        public SortedDictionary<long, string> Rows { get; } = [];

        public Task<IReadOnlyList<TimePeriod>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TimePeriod>>(
                Rows.Select(r => new TimePeriod { Id = r.Key, Name = r.Value }).ToList());

        public Task<TimePeriod?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.TryGetValue(id, out var name) ? new TimePeriod { Id = id, Name = name } : null);

        public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.ContainsKey(id));

        public Task AddAsync(TimePeriod timePeriod, CancellationToken cancellationToken)
        {
            Rows.Add(timePeriod.Id, timePeriod.Name);
            return Task.CompletedTask;
        }

        public Task<int> UpdateAsync(long id, long newId, string name, CancellationToken cancellationToken)
        {
            if (!Rows.Remove(id))
            {
                return Task.FromResult(0);
            }

            Rows[newId] = name;
            return Task.FromResult(1);
        }

        public Task<int> DeleteAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.Remove(id) ? 1 : 0);
    }
}
