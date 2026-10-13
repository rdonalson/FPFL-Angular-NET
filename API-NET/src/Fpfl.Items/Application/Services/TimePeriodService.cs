using Fpfl.Common.Exceptions;
using Fpfl.Items.Domain.Entities;
using Fpfl.Items.Domain.Repositories;
using Fpfl.Items.Domain.Services;

namespace Fpfl.Items.Application.Services;

/// <summary>
///     Business rules for time periods (port of API-JDK's TimePeriodServiceImpl).
///     Messages match the Java service so both APIs read the same to clients.
/// </summary>
public sealed class TimePeriodService(ITimePeriodRepository repository) : ITimePeriodService
{
    public const int MaxNameLength = 75;

    public Task<IReadOnlyList<TimePeriod>> ListAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);

    public async Task<TimePeriod> GetAsync(long id, CancellationToken cancellationToken)
    {
        ValidateId(id);

        return await repository.FindByIdAsync(id, cancellationToken)
               ?? throw new ItemNotFoundException($"TimePeriod {id} not found");
    }

    public async Task<TimePeriod> CreateAsync(TimePeriod timePeriod, CancellationToken cancellationToken)
    {
        ValidateId(timePeriod.Id);
        timePeriod.Name = ValidateName(timePeriod.Name);

        if (await repository.ExistsAsync(timePeriod.Id, cancellationToken))
        {
            throw new DuplicateItemException($"TimePeriod {timePeriod.Id} already exists.");
        }

        await repository.AddAsync(timePeriod, cancellationToken);
        return timePeriod;
    }

    public async Task<TimePeriod> UpdateAsync(long id, long? newId, string name, CancellationToken cancellationToken)
    {
        ValidateId(id);
        var targetId = newId ?? id;
        ValidateId(targetId);
        name = ValidateName(name);

        if (targetId != id && await repository.ExistsAsync(targetId, cancellationToken))
        {
            throw new DuplicateItemException($"TimePeriod {targetId} already exists.");
        }

        var updated = await repository.UpdateAsync(id, targetId, name, cancellationToken);
        if (updated == 0)
        {
            throw new ItemNotFoundException($"TimePeriod {id} not found");
        }

        return new TimePeriod { Id = targetId, Name = name };
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        ValidateId(id);

        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (deleted == 0)
        {
            throw new ItemNotFoundException($"TimePeriod {id} not found");
        }
    }

    private static void ValidateId(long id)
    {
        if (id <= 0)
        {
            throw new DomainValidationException("TimePeriod id must be a positive integer");
        }
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("TimePeriod Name is required and cannot be blank");
        }

        name = name.Trim();
        if (name.Length > MaxNameLength)
        {
            throw new DomainValidationException($"TimePeriod Name must be at most {MaxNameLength} characters");
        }

        return name;
    }
}
