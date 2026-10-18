using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Fpfl.Items.Infrastructure.Persistence.Repositories;

/// <summary>
///     EF Core implementation of <see cref="IItemTypeRepository" />. PostgreSQL constraint
///     violations become domain exceptions (409 / 400); other failures become
///     <see cref="RepositoryException" /> (a clean 500 with a correlation id).
/// </summary>
public sealed class ItemTypeRepository(ItemsDbContext db) : IItemTypeRepository
{
    public async Task<IReadOnlyList<ItemType>> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await db.ItemTypes.OrderBy(t => t.Id).ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException("Database failure while fetching ItemTypes", ex);
        }
    }

    public async Task<ItemType?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await db.ItemTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching ItemType {id}", ex);
        }
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await db.ItemTypes.AnyAsync(t => t.Id == id, cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching ItemType {id}", ex);
        }
    }

    public async Task AddAsync(ItemType itemType, CancellationToken cancellationToken)
    {
        try
        {
            db.ItemTypes.Add(itemType);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.UniqueViolation)
        {
            // Lost a race with another create for the same id.
            throw new DuplicateItemException($"ItemType {itemType.Id} already exists.");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while saving ItemType {itemType.Id}", ex);
        }
        finally
        {
            db.ChangeTracker.Clear(); // the context is no-tracking by default; keep it that way
        }
    }

    public async Task<int> UpdateAsync(long id, long newId, string name, CancellationToken cancellationToken)
    {
        try
        {
            // Raw (parameterized) SQL because EF won't change a primary key through tracking.
            return await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE fpfl.item_types SET id = {newId}, name = {name} WHERE id = {id}",
                cancellationToken);
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateItemException($"ItemType {newId} already exists.");
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new DomainValidationException($"ItemType {id} is used by existing items, so its ID cannot be changed");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while saving ItemType {id}", ex);
        }
    }

    public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await db.ItemTypes.Where(t => t.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new DomainValidationException($"ItemType {id} is used by existing items and cannot be deleted");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while deleting ItemType {id}", ex);
        }
    }

    private static string? SqlState(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException)?.SqlState;

    private static bool IsDatabaseFailure(Exception ex) =>
        ex is not OperationCanceledException and not DomainException;
}
