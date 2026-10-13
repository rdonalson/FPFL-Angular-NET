using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fpfl.Items.Infrastructure.Persistence.Repositories;

/// <summary>
///     EF Core implementation of <see cref="ITimePeriodRepository" />. PostgreSQL constraint
///     violations become domain exceptions (409 / 400); other failures become
///     <see cref="RepositoryException" /> (a clean 500 with a correlation id).
/// </summary>
public sealed class TimePeriodRepository(ItemsDbContext db) : ITimePeriodRepository
{
    public async Task<IReadOnlyList<TimePeriod>> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await db.TimePeriods.OrderBy(t => t.Id).ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException("Database failure while fetching TimePeriods", ex);
        }
    }

    public async Task<TimePeriod?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await db.TimePeriods.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching TimePeriod {id}", ex);
        }
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await db.TimePeriods.AnyAsync(t => t.Id == id, cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching TimePeriod {id}", ex);
        }
    }

    public async Task AddAsync(TimePeriod timePeriod, CancellationToken cancellationToken)
    {
        try
        {
            db.TimePeriods.Add(timePeriod);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.UniqueViolation)
        {
            // Lost a race with another create for the same id.
            throw new DuplicateItemException($"TimePeriod {timePeriod.Id} already exists.");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while saving TimePeriod {timePeriod.Id}", ex);
        }
        finally
        {
            db.ChangeTracker.Clear(); // the context is no-tracking by default; keep it that way
        }
    }

    public async Task<int> UpdateNameAsync(long id, string name, CancellationToken cancellationToken)
    {
        try
        {
            return await db.TimePeriods
                .Where(t => t.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Name, name), cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while saving TimePeriod {id}", ex);
        }
    }

    public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await db.TimePeriods.Where(t => t.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new DomainValidationException($"TimePeriod {id} is used by existing items and cannot be deleted");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while deleting TimePeriod {id}", ex);
        }
    }

    private static string? SqlState(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException)?.SqlState;

    private static bool IsDatabaseFailure(Exception ex) =>
        ex is not OperationCanceledException and not DomainException;
}
