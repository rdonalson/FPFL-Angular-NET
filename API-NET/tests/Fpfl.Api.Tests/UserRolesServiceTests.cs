using Fpfl.Auth.Application.Services;
using Fpfl.Auth.Domain.Entities;
using Fpfl.Auth.Domain.Repositories;
using Fpfl.Common.Exceptions;

namespace Fpfl.Api.Tests;

public sealed class UserRolesServiceTests
{
    private const long AdminRoleId = 1;
    private const long UserRoleId = 2;
    private const long Admin = 10;
    private const long Member = 20;

    private readonly FakeStore _store = new();
    private readonly UserRolesService _service;

    public UserRolesServiceTests()
    {
        _service = new UserRolesService(_store, _store);
        _store.Roles[AdminRoleId] = "ROLE_ADMIN";
        _store.Roles[UserRoleId] = "ROLE_USER";
        _store.Users.Add(Admin);
        _store.Users.Add(Member);
        _store.Mappings.Add((Admin, AdminRoleId));
        _store.Mappings.Add((Admin, UserRoleId));
        _store.Mappings.Add((Member, UserRoleId));
    }

    [Fact]
    public async Task Get_returns_role_names()
    {
        var roles = await _service.GetRoleNamesAsync(Admin, default);

        Assert.Equal(new[] { "ROLE_ADMIN", "ROLE_USER" }, roles);
    }

    [Fact]
    public async Task Unknown_user_is_not_found()
    {
        var error = await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.GetRoleNamesAsync(99, default));
        Assert.Equal("User 99 not found", error.Message);
    }

    [Fact]
    public async Task Add_is_idempotent()
    {
        await _service.AddRoleAsync(Member, AdminRoleId, default);
        await _service.AddRoleAsync(Member, AdminRoleId, default);

        Assert.Single(_store.Mappings, m => m == (Member, AdminRoleId));
    }

    [Fact]
    public async Task Adding_an_unknown_role_is_not_found()
    {
        var error = await Assert.ThrowsAsync<ItemNotFoundException>(() => _service.AddRoleAsync(Member, 7, default));
        Assert.Equal("Role 7 not found", error.Message);
    }

    [Fact]
    public async Task Remove_reports_whether_the_role_was_assigned()
    {
        Assert.True(await _service.RemoveRoleAsync(Admin, Member, UserRoleId, default));
        Assert.False(await _service.RemoveRoleAsync(Admin, Member, UserRoleId, default));
    }

    [Fact]
    public async Task Admin_cannot_remove_their_own_admin_role()
    {
        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => _service.RemoveRoleAsync(Admin, Admin, AdminRoleId, default));
        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => _service.ReplaceRolesAsync(Admin, Admin, [UserRoleId], default));
    }

    [Fact]
    public async Task Replace_sets_exactly_the_given_roles()
    {
        await _service.ReplaceRolesAsync(Admin, Member, [AdminRoleId, UserRoleId, AdminRoleId], default);

        Assert.Equal(new[] { "ROLE_ADMIN", "ROLE_USER" }, await _service.GetRoleNamesAsync(Member, default));
    }

    [Fact]
    public async Task Replace_with_empty_list_clears_roles()
    {
        await _service.ReplaceRolesAsync(Admin, Member, [], default);

        Assert.Empty(await _service.GetRoleNamesAsync(Member, default));
    }

    [Fact]
    public async Task Replace_with_unknown_roles_changes_nothing()
    {
        var error = await Assert.ThrowsAsync<ItemNotFoundException>(
            () => _service.ReplaceRolesAsync(Admin, Member, [UserRoleId, 8, 7], default));

        Assert.Equal("Roles 7, 8 not found", error.Message);
        Assert.Equal(new[] { "ROLE_USER" }, await _service.GetRoleNamesAsync(Member, default));
    }

    [Fact]
    public async Task Replace_with_null_is_a_validation_error()
    {
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _service.ReplaceRolesAsync(Admin, Member, null, default));
    }

    /// <summary>In-memory users, roles and user_roles behind both repository interfaces.</summary>
    private sealed class FakeStore : IUserRepository, IUserRolesRepository
    {
        public HashSet<long> Users { get; } = [];
        public Dictionary<long, string> Roles { get; } = [];
        public List<(long UserId, long RoleId)> Mappings { get; } = [];

        // IUserRepository
        public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.Contains(id) ? new User { Id = id, Email = $"user{id}@example.com" } : null);

        public Task<IReadOnlyList<string>> GetRoleNamesAsync(long userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(Mappings
                .Where(m => m.UserId == userId)
                .Select(m => Roles[m.RoleId])
                .Order()
                .ToList());

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<User> AddWithRoleAsync(User user, string roleName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // IUserRolesRepository
        public Task<bool> ExistsAsync(long userId, long roleId, CancellationToken cancellationToken) =>
            Task.FromResult(Mappings.Contains((userId, roleId)));

        public Task<IReadOnlyList<long>> FindExistingRoleIdsAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<long>>(roleIds.Where(Roles.ContainsKey).ToList());

        public Task<long?> FindRoleIdByNameAsync(string roleName, CancellationToken cancellationToken) =>
            Task.FromResult(Roles.Where(r => r.Value == roleName).Select(r => (long?)r.Key).FirstOrDefault());

        public Task AddAsync(long userId, long roleId, CancellationToken cancellationToken)
        {
            Mappings.Add((userId, roleId));
            return Task.CompletedTask;
        }

        public Task<int> RemoveAsync(long userId, long roleId, CancellationToken cancellationToken) =>
            Task.FromResult(Mappings.Remove((userId, roleId)) ? 1 : 0);

        public Task ReplaceAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken)
        {
            Mappings.RemoveAll(m => m.UserId == userId);
            Mappings.AddRange(roleIds.Select(roleId => (userId, roleId)));
            return Task.CompletedTask;
        }
    }
}
