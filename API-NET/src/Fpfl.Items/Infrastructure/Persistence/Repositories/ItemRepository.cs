using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fpfl.Items.Infrastructure.Persistence.Repositories;

/// <summary>
///     EF Core implementation of <see cref="IItemRepository" />. Every query loads the item type and
///     time period with the item; database failures become <see cref="RepositoryException" />.
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

    private IQueryable<Item> WithLookups() =>
        db.Items.Include(i => i.ItemType).Include(i => i.TimePeriod);

    private static bool IsDatabaseFailure(Exception ex) =>
        ex is not OperationCanceledException and not DomainException;
}
