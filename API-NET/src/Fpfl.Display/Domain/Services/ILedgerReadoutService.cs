using Fpfl.Display.Domain.Models;

namespace Fpfl.Display.Domain.Services;

/// <summary>Builds a user's forecast ledger. Port of API-JDK's <c>LedgerReadoutService</c>.</summary>
public interface ILedgerReadoutService
{
    /// <summary>The longest ledger that can be requested in one call.</summary>
    const int MaxYears = 10;

    /// <returns>One row per day from <paramref name="start" /> to <paramref name="end" />, inclusive.</returns>
    /// <exception cref="Fpfl.Common.Exceptions.ForbiddenOperationException">Another user's ledger, and the caller isn't an admin.</exception>
    /// <exception cref="Fpfl.Common.Exceptions.DomainValidationException">The dates are out of order, or span more than <see cref="MaxYears" /> years.</exception>
    Task<IReadOnlyList<LedgerDay>> BuildAsync(
        Guid userId,
        DateOnly start,
        DateOnly end,
        Guid requesterUserId,
        bool requesterIsAdmin,
        CancellationToken cancellationToken);
}
