using Fpfl.Auth.Domain.Entities;

namespace Fpfl.Auth.Domain.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>Finds a token by its hash, including its user.</summary>
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(RefreshToken token);

    void Remove(RefreshToken token);

    Task RemoveAllForUserAsync(long userId, CancellationToken cancellationToken);

    Task RemoveExpiredForUserAsync(long userId, DateTime nowUtc, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
