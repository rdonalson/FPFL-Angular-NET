using Fpfl.Auth.Application.Models;

namespace Fpfl.Auth.Application.Services;

public interface IAuthService
{
    Task<AuthSession> LoginAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>Signs in as the configured guest account without a password.</summary>
    Task<AuthSession> GuestLoginAsync(CancellationToken cancellationToken);

    Task<AuthSession> RegisterAsync(
        string email, string password, string first, string last, CancellationToken cancellationToken);

    /// <summary>Exchanges a refresh token for a new session. The old refresh token stops working.</summary>
    Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes the refresh token. Succeeds even if it is unknown or already expired.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task<AuthSession> ChangePasswordAsync(
        long userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
}
