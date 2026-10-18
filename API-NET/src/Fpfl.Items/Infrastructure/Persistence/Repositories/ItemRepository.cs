using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Fpfl.Items.Infrastructure.Persistence.Repositories;

/// <summary>
///     EF Core implementation of <see cref="IItemRepository" />. Every query loads the item type and
///     time period with the item; a missing item type or time period on save becomes a 400, and other
///     database failures become <see cref="RepositoryException" />.
/// </summary>
public sealed class ItemRepository(ItemsDbContext db) : IItemRepository
{
    public async Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await WithLookups().OrderBy(i => i.Name).ThenBy(i => i.Id).ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException("Database failure while fetching Items", ex);
        }
    }

    public async Task<Item?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await WithLookups().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching Item {id}", ex);
        }
    }

    public async Task<IReadOnlyList<Item>> ListByUserAndTypeAsync(
        Guid userId, long itemTypeId, CancellationToken cancellationToken)
    {
        try
        {
            return await WithLookups()
                .Where(i => i.UserId == userId && i.ItemTypeId == itemTypeId)
                .OrderBy(i => i.Name).ThenBy(i => i.Id)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching Items of type {itemTypeId}", ex);
        }
    }

    public async Task<IReadOnlyList<Item>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            return await WithLookups()
                .Where(i => i.UserId == userId)
                .OrderBy(i => i.Id)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching Items for user {userId}", ex);
        }
    }

    public async Task<long> AddAsync(Item item, CancellationToken cancellationToken)
    {
        try
        {
            db.Items.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return item.Id;
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new DomainValidationException("The item type or time period does not exist");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException("Database failure while saving Item", ex);
        }
        finally
        {
            db.ChangeTracker.Clear(); // the context is no-tracking by default; keep it that way
        }
    }

    public async Task UpdateAsync(Item item, CancellationToken cancellationToken)
    {
        try
        {
            db.Items.Update(item); // marks every column modified, so cleared recurrence fields are written as NULL
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new DomainValidationException("The item type or time period does not exist");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while saving Item {item.Id}", ex);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await db.Items.Where(i => i.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while deleting Item {id}", ex);
        }
    }

    private static string? SqlState(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException)?.SqlState;

    private IQueryable<Item> WithLookups() =>
        db.Items.Include(i => i.ItemType).Include(i => i.TimePeriod);

    private static bool IsDatabaseFailure(Exception ex) =>
        ex is not OperationCanceledException and not DomainException;
}
