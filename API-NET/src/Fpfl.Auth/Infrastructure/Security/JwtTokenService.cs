using System.Buffers.Text;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Fpfl.Auth.Application.Security;
using Fpfl.Auth.Domain.Entities;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fpfl.Auth.Infrastructure.Security;

/// <summary>
///     Issues HS256 access tokens and opaque refresh tokens.
/// </summary>
public sealed class JwtTokenService(IOptions<AuthOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(
        User user, IReadOnlyList<string> roles, bool isGuest)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(AuthClaims.Subject, user.Email),
            new(AuthClaims.Id, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(AuthClaims.UserId, user.UserId.ToString()),
            new(AuthClaims.Guest, isGuest ? "true" : "false")
        };
        claims.AddRange(roles.Select(role => new Claim(AuthClaims.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return (_handler.CreateToken(descriptor), expiresAt);
    }

    public string CreateRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
