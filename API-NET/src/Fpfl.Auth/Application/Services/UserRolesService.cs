using Fpfl.Auth.Domain.Repositories;
using Fpfl.Common.Exceptions;

namespace Fpfl.Auth.Application.Services;

/// <summary>
///     Port of API-JDK's UserRolesServiceImpl, with two additions: unknown users and roles are
///     a 404 instead of a database error, and an admin cannot remove their own admin role
///     (which would leave nobody able to undo it).
/// </summary>
public sealed class UserRolesService(IUserRepository users, IUserRolesRepository userRoles) : IUserRolesService
{
    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(long userId, CancellationToken cancellationToken)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        return await users.GetRoleNamesAsync(userId, cancellationToken);
    }

    public async Task AddRoleAsync(long userId, long roleId, CancellationToken cancellationToken)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        await EnsureRolesExistAsync([roleId], cancellationToken);

        if (!await userRoles.ExistsAsync(userId, roleId, cancellationToken))
        {
            await userRoles.AddAsync(userId, roleId, cancellationToken);
        }
    }

    public async Task<bool> RemoveRoleAsync(long actingUserId, long userId, long roleId, CancellationToken cancellationToken)
    {
        ValidateId(roleId, "Role");
        await EnsureUserExistsAsync(userId, cancellationToken);

        if (actingUserId == userId && roleId == await AdminRoleIdAsync(cancellationToken))
        {
            throw new ForbiddenOperationException("You cannot remove your own admin role");
        }

        return await userRoles.RemoveAsync(userId, roleId, cancellationToken) > 0;
    }

    public async Task ReplaceRolesAsync(
        long actingUserId, long userId, IReadOnlyCollection<long>? roleIds, CancellationToken cancellationToken)
    {
        if (roleIds is null)
        {
            throw new DomainValidationException("roleIds must not be null. Use an empty list to clear roles.");
        }

        var distinct = roleIds.Distinct().ToList();
        await EnsureUserExistsAsync(userId, cancellationToken);
        await EnsureRolesExistAsync(distinct, cancellationToken);

        if (actingUserId == userId)
        {
            var adminRoleId = await AdminRoleIdAsync(cancellationToken);
            if (adminRoleId is not null && !distinct.Contains(adminRoleId.Value))
            {
                throw new ForbiddenOperationException("You cannot remove your own admin role");
            }
        }

        await userRoles.ReplaceAsync(userId, distinct, cancellationToken);
    }

    private async Task EnsureUserExistsAsync(long userId, CancellationToken cancellationToken)
    {
        ValidateId(userId, "User");
        _ = await users.FindByIdAsync(userId, cancellationToken)
            ?? throw new ItemNotFoundException($"User {userId} not found");
    }

    private async Task EnsureRolesExistAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken)
    {
        foreach (var roleId in roleIds)
        {
            ValidateId(roleId, "Role");
        }

        if (roleIds.Count == 0)
        {
            return;
        }

        var existing = await userRoles.FindExistingRoleIdsAsync(roleIds, cancellationToken);
        var missing = roleIds.Except(existing).Order().ToList();
        if (missing.Count > 0)
        {
            throw new ItemNotFoundException(missing.Count == 1
                ? $"Role {missing[0]} not found"
                : $"Roles {string.Join(", ", missing)} not found");
        }
    }

    private Task<long?> AdminRoleIdAsync(CancellationToken cancellationToken) =>
        userRoles.FindRoleIdByNameAsync(AuthClaims.AdminRole, cancellationToken);

    private static void ValidateId(long id, string what)
    {
        if (id <= 0)
        {
            throw new DomainValidationException($"{what} id must be a positive integer");
        }
    }
}
