using Fpfl.Auth.Domain.Entities;
using Fpfl.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fpfl.Auth.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(AuthDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);

    public void Remove(RefreshToken token) => db.RefreshTokens.Remove(token);

    public Task RemoveAllForUserAsync(long userId, CancellationToken cancellationToken) =>
        db.RefreshTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(cancellationToken);

    public Task RemoveExpiredForUserAsync(long userId, DateTime nowUtc, CancellationToken cancellationToken) =>
        db.RefreshTokens.Where(t => t.UserId == userId && t.ExpiryDate <= nowUtc).ExecuteDeleteAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
