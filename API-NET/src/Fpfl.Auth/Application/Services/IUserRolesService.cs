namespace Fpfl.Auth.Application.Services;

/// <summary>
///     Admin operations on a user's roles. Changes reach the user's access token at its next
///     refresh (within <see cref="AuthOptions.AccessTokenMinutes" />).
/// </summary>
public interface IUserRolesService
{
    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">The user does not exist.</exception>
    Task<IReadOnlyList<string>> GetRoleNamesAsync(long userId, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">The user or role does not exist.</exception>
    Task AddRoleAsync(long userId, long roleId, CancellationToken cancellationToken);

    /// <returns>False when the role was not assigned to the user.</returns>
    /// <exception cref="Fpfl.Common.Exceptions.ForbiddenOperationException">An admin removing their own admin role.</exception>
    Task<bool> RemoveRoleAsync(long actingUserId, long userId, long roleId, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">The user or a role does not exist.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.ForbiddenOperationException">An admin removing their own admin role.</exception>
    Task ReplaceRolesAsync(long actingUserId, long userId, IReadOnlyCollection<long>? roleIds, CancellationToken cancellationToken);
}
