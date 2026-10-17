using Fpfl.Common.Exceptions;
using Fpfl.Display.Domain;
using Fpfl.Display.Domain.Models;
using Fpfl.Display.Domain.Recurrence;
using Fpfl.Display.Domain.Services;
using Fpfl.Items.Domain.Repositories;

namespace Fpfl.Display.Application.Services;

/// <summary>
///     Loads the user's items and hands them to <see cref="LedgerBuilder" />. Unlike API-JDK, which built
///     a ledger for whatever userId the request body named, a user can only build their own (admins any),
///     and the range is capped so one request can't ask for centuries of rows.
/// </summary>
public sealed class LedgerReadoutService(IItemRepository repository) : ILedgerReadoutService
{
    public async Task<IReadOnlyList<LedgerDay>> BuildAsync(
        Guid userId,
        DateOnly start,
        DateOnly end,
        Guid requesterUserId,
        bool requesterIsAdmin,
        CancellationToken cancellationToken)
    {
        if (!requesterIsAdmin && userId != requesterUserId)
        {
            throw new ForbiddenOperationException("You can only view your own ledger");
        }

        if (end < start)
        {
            throw new DomainValidationException("Ledger start date must be on or before the end date");
        }

        if (end > start.AddYears(ILedgerReadoutService.MaxYears))
        {
            throw new DomainValidationException($"A ledger can cover at most {ILedgerReadoutService.MaxYears} years");
        }

        var items = await repository.ListByUserAsync(userId, cancellationToken);
        return LedgerBuilder.Build(items, new DateWindow(start, end));
    }
}
