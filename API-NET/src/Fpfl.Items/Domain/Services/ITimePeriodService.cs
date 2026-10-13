using Fpfl.Items.Domain.Entities;

namespace Fpfl.Items.Domain.Services;

public interface ITimePeriodService
{
    Task<IReadOnlyList<TimePeriod>> ListAsync(CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">id is not positive.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No time period with that id.</exception>
    Task<TimePeriod> GetAsync(long id, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.DuplicateItemException">The id already exists.</exception>
    Task<TimePeriod> CreateAsync(TimePeriod timePeriod, CancellationToken cancellationToken);

    /// <summary>Renames the time period and, when <paramref name="newId" /> is given and different, changes its id.</summary>
    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No time period with that id.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DuplicateItemException">The new id already exists.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">The id changes but items still use it.</exception>
    Task<TimePeriod> UpdateAsync(long id, long? newId, string name, CancellationToken cancellationToken);

    /// <exception cref="Fpfl.Common.Exceptions.ItemNotFoundException">No time period with that id.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">Items still use this time period.</exception>
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
