using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fpfl.Items.Infrastructure.Persistence.Repositories;

/// <summary>
///     EF Core implementation of <see cref="IItemTypeRepository" />. Database failures are
///     wrapped in <see cref="RepositoryException" /> so they surface as a clean 500.
/// </summary>
public sealed class ItemTypeRepository(ItemsDbContext db) : IItemTypeRepository
{
    public async Task<IReadOnlyList<ItemType>> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await db.ItemTypes.OrderBy(t => t.Id).ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new RepositoryException("Failed to read item types", ex);
        }
    }

    public async Task<ItemType?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await db.ItemTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new RepositoryException($"Failed to read item type {id}", ex);
        }
    }
}
