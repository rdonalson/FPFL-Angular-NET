namespace Fpfl.Auth.Domain.Repositories;

/// <summary>
///     user_roles mappings (port of API-JDK's UserRolesRepository).
///     Role names for a user come from <see cref="IUserRepository.GetRoleNamesAsync" />.
/// </summary>
public interface IUserRolesRepository
{
    Task<bool> ExistsAsync(long userId, long roleId, CancellationToken cancellationToken);

    /// <summary>Returns which of <paramref name="roleIds" /> exist in fpfl.roles.</summary>
    Task<IReadOnlyList<long>> FindExistingRoleIdsAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken);

    Task<long?> FindRoleIdByNameAsync(string roleName, CancellationToken cancellationToken);

    /// <summary>Adds the mapping; does nothing if it already exists.</summary>
    Task AddAsync(long userId, long roleId, CancellationToken cancellationToken);

    /// <returns>Rows deleted (0 or 1).</returns>
    Task<int> RemoveAsync(long userId, long roleId, CancellationToken cancellationToken);

    /// <summary>Deletes every mapping for the user and inserts <paramref name="roleIds" />, in one transaction.
    /// An empty list clears all roles.</summary>
    Task ReplaceAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken);
}
