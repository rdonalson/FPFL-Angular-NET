using Fpfl.Auth.Domain.Entities;

namespace Fpfl.Auth.Application.Security;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, IReadOnlyList<string> roles, bool isGuest);

    /// <summary>A new random refresh token (raw value, returned to the client once).</summary>
    string CreateRefreshToken();

    /// <summary>The value stored in the database for a raw refresh token.</summary>
    string HashRefreshToken(string refreshToken);
}
