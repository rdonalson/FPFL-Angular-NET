using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Repositories;

public interface ITimePeriodRepository
{
    Task<IReadOnlyList<TimePeriod>> ListAsync(CancellationToken cancellationToken);

    Task<TimePeriod?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DuplicateItemException">The id is already taken.</exception>
    Task AddAsync(TimePeriod timePeriod, CancellationToken cancellationToken);

    /// <summary>Sets the id and name of the row currently at <paramref name="id" />.</summary>
    /// <returns>The number of rows updated (0 when the id does not exist).</returns>
    /// <exception cref="Fpfl.Common.Exceptions.DuplicateItemException"><paramref name="newId" /> is already taken.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">The id changes but items still use the old one.</exception>
    Task<int> UpdateAsync(long id, long newId, string name, CancellationToken cancellationToken);

    /// <returns>The number of rows deleted (0 when the id does not exist).</returns>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">Items still use this time period.</exception>
    Task<int> DeleteAsync(long id, CancellationToken cancellationToken);
}
