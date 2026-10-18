using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fpfl.Items.Infrastructure.Persistence.Repositories;

/// <summary>
///     EF Core implementation of <see cref="IDebtRepository" /> over <c>fpfl.debts</c> and
///     <c>fpfl.debt_allocations</c> (see <c>db/001_debt_paydown.sql</c>).
/// </summary>
public sealed class DebtRepository(ItemsDbContext db) : IDebtRepository
{
    public async Task<IReadOnlyList<Debt>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            return await WithDetails().Where(d => d.UserId == userId).OrderBy(d => d.Id).ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching debts for user {userId}", ex);
        }
    }

    public async Task<Debt?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await WithDetails().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while fetching debt {id}", ex);
        }
    }

    public async Task<long> AddAsync(Debt debt, CancellationToken cancellationToken)
    {
        try
        {
            db.Debts.Add(debt);
            await db.SaveChangesAsync(cancellationToken);
            return debt.Id;
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateItemException("That debit is already being tracked");
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new DomainValidationException("The debit or credit no longer exists");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException("Database failure while saving debt", ex);
        }
        finally
        {
            db.ChangeTracker.Clear(); // the context is no-tracking by default; keep it that way
        }
    }

    public async Task UpdateAsync(Debt debt, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await db.Debts.AsTracking().Include(d => d.Allocations)
                .FirstOrDefaultAsync(d => d.Id == debt.Id, cancellationToken)
                ?? throw new ItemNotFoundException($"Debt {debt.Id} not found");

            existing.Balance = debt.Balance;
            existing.Apr = debt.Apr;
            existing.BalanceDate = debt.BalanceDate;

            // Diff the allocations rather than replace them, so (debt, credit) stays unique at every step.
            var wanted = debt.Allocations.ToDictionary(a => a.CreditItemId);
            foreach (var current in existing.Allocations.ToList())
            {
                if (wanted.Remove(current.CreditItemId, out var change))
                {
                    current.Percent = change.Percent;
                }
                else
                {
                    existing.Allocations.Remove(current);
                    db.DebtAllocations.Remove(current);
                }
            }

            foreach (var added in wanted.Values)
            {
                existing.Allocations.Add(new DebtAllocation { CreditItemId = added.CreditItemId, Percent = added.Percent });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (SqlState(ex) == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new DomainValidationException("The credit no longer exists");
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while saving debt {debt.Id}", ex);
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
            return await db.Debts.Where(d => d.Id == id).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            throw new RepositoryException($"Database failure while deleting debt {id}", ex);
        }
    }

    private IQueryable<Debt> WithDetails() =>
        db.Debts
            .Include(d => d.Item).ThenInclude(i => i!.TimePeriod)
            .Include(d => d.Allocations).ThenInclude(a => a.CreditItem).ThenInclude(i => i!.TimePeriod)
            .AsSplitQuery();

    private static string? SqlState(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException)?.SqlState;

    private static bool IsDatabaseFailure(Exception ex) =>
        ex is not OperationCanceledException and not DomainException;
}
