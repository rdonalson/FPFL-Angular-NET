using Fpfl.Auth.Application.Models;
using Fpfl.Auth.Application.Security;
using Fpfl.Auth.Domain.Entities;
using Fpfl.Auth.Domain.Repositories;
using Fpfl.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Fpfl.Auth.Application.Services;

/// <summary>
///     Port of API-JDK's AuthServiceImpl + RefreshTokenServiceImpl, with these differences:
///     guest login happens server-side, refresh tokens are single-use and stored hashed,
///     and change-password is refused for the guest account.
/// </summary>
public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<AuthOptions> options,
    TimeProvider clock) : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password";

    private AuthOptions Options => options.Value;

    public async Task<AuthSession> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email.Trim(), cancellationToken);

        // Same message for unknown email and wrong password, so accounts can't be probed.
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new InvalidCredentialsException(InvalidCredentialsMessage);
        }

        return await IssueSessionAsync(user, cancellationToken);
    }

    public async Task<AuthSession> GuestLoginAsync(CancellationToken cancellationToken)
    {
        var guest = await users.FindByEmailAsync(Options.GuestEmail, cancellationToken)
                    ?? throw new ItemNotFoundException("The guest account is not available");

        return await IssueSessionAsync(guest, cancellationToken);
    }

    public async Task<AuthSession> RegisterAsync(
        string email, string password, string first, string last, CancellationToken cancellationToken)
    {
        email = email.Trim();

        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new DuplicateItemException("An account with this email already exists");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            UserId = Guid.NewGuid(),
            First = first.Trim(),
            Last = last.Trim()
        };

        user = await users.AddWithRoleAsync(user, AuthClaims.UserRole, cancellationToken);
        return await IssueSessionAsync(user, cancellationToken);
    }

    public async Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await refreshTokens.FindByHashAsync(tokenService.HashRefreshToken(refreshToken), cancellationToken)
                     ?? throw new InvalidCredentialsException("Invalid refresh token");

        // Single use: the presented token is deleted whether it is expired or not.
        refreshTokens.Remove(stored);

        if (stored.ExpiryDate <= clock.GetUtcNow().UtcDateTime)
        {
            await refreshTokens.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException("Refresh token expired");
        }

        return await IssueSessionAsync(stored.User, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await refreshTokens.FindByHashAsync(tokenService.HashRefreshToken(refreshToken), cancellationToken);
        if (stored is null)
        {
            return;
        }

        refreshTokens.Remove(stored);
        await refreshTokens.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthSession> ChangePasswordAsync(
        long userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken)
                   ?? throw new ItemNotFoundException("User not found");

        if (IsGuest(user))
        {
            throw new ForbiddenOperationException("The guest account's password cannot be changed");
        }

        // 400, not 401: a 401 would make the UI think the session expired.
        if (!passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            throw new DomainValidationException("Current password is incorrect");
        }

        if (currentPassword == newPassword)
        {
            throw new DomainValidationException("New password must be different from the current password");
        }

        user.PasswordHash = passwordHasher.Hash(newPassword);
        await users.SaveChangesAsync(cancellationToken);

        // Sign out every other device, then issue a fresh session for this one.
        await refreshTokens.RemoveAllForUserAsync(user.Id, cancellationToken);
        return await IssueSessionAsync(user, cancellationToken);
    }

    private async Task<AuthSession> IssueSessionAsync(User user, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var roles = await users.GetRoleNamesAsync(user.Id, cancellationToken);
        var isGuest = IsGuest(user);

        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user, roles, isGuest);
        var refreshToken = tokenService.CreateRefreshToken();

        await refreshTokens.RemoveExpiredForUserAsync(user.Id, now, cancellationToken);
        refreshTokens.Add(new RefreshToken
        {
            Token = tokenService.HashRefreshToken(refreshToken),
            UserId = user.Id,
            ExpiryDate = now.AddDays(Options.RefreshTokenDays)
        });
        await refreshTokens.SaveChangesAsync(cancellationToken);

        return new AuthSession(accessToken, expiresAt, refreshToken, user, roles, isGuest);
    }

    private bool IsGuest(User user) =>
        string.Equals(user.Email, Options.GuestEmail, StringComparison.OrdinalIgnoreCase);
}
