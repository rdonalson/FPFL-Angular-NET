using Fpfl.Auth.Domain.Entities;
using Fpfl.Auth.Domain.Repositories;
using Fpfl.Common.Exceptions;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Fpfl.Auth.Infrastructure.Persistence.Repositories;

/// <summary>
///     EF Core implementation of <see cref="IUserRolesRepository" />. Database failures are
///     wrapped in <see cref="RepositoryException" /> so they surface as a clean 500.
/// </summary>
public sealed class UserRolesRepository(AuthDbContext db) : IUserRolesRepository
{
    public Task<bool> ExistsAsync(long userId, long roleId, CancellationToken cancellationToken) =>
        Guard($"reading roles for user {userId}",
            () => db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken));

    public Task<IReadOnlyList<long>> FindExistingRoleIdsAsync(
        IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken) =>
        Guard("reading roles", async () => (IReadOnlyList<long>)await db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken));

    public Task<long?> FindRoleIdByNameAsync(string roleName, CancellationToken cancellationToken) =>
        Guard($"reading role {roleName}", () => db.Roles
            .Where(r => r.Name == roleName)
            .Select(r => (long?)r.Id)
            .FirstOrDefaultAsync(cancellationToken));

    public async Task AddAsync(long userId, long roleId, CancellationToken cancellationToken)
    {
        try
        {
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.UniqueViolation)
        {
            // Added concurrently by another request: the end state is what was asked for.
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while adding role {roleId} to user {userId}", ex);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    public Task<int> RemoveAsync(long userId, long roleId, CancellationToken cancellationToken) =>
        Guard($"removing role {roleId} from user {userId}", () => db.UserRoles
            .Where(ur => ur.UserId == userId && ur.RoleId == roleId)
            .ExecuteDeleteAsync(cancellationToken));

    public async Task ReplaceAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            await db.UserRoles.Where(ur => ur.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            if (roleIds.Count > 0)
            {
                db.UserRoles.AddRange(roleIds.Select(roleId => new UserRole { UserId = userId, RoleId = roleId }));
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while replacing roles for user {userId}", ex);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    private static async Task<T> Guard<T>(string action, Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while {action}", ex);
        }
    }

    private static string? SqlState(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException)?.SqlState;

    private static bool IsDatabaseFailure(Exception ex) =>
        ex is not OperationCanceledException and not DomainException;
}
