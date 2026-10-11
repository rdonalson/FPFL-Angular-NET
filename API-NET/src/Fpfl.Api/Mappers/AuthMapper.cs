using System.Globalization;
using System.Security.Claims;
using Fpfl.Api.Dtos.Auth;
using Fpfl.Auth;
using Fpfl.Auth.Application.Models;
using Fpfl.Common.Exceptions;

namespace Fpfl.Api.Mappers;

public sealed class AuthMapper
{
    public AuthResponse ToResponse(AuthSession session) => new(
        session.AccessToken,
        session.AccessTokenExpiresAt,
        session.RefreshToken,
        session.User.Id,
        session.User.Email,
        session.User.UserId,
        session.User.First,
        session.User.Last,
        session.Roles,
        session.IsGuest);

    public CurrentUserResponse ToCurrentUser(ClaimsPrincipal principal) => new(
        GetUserId(principal),
        Guid.Parse(principal.FindFirstValue(AuthClaims.UserId) ?? Guid.Empty.ToString()),
        principal.FindFirstValue(AuthClaims.Subject) ?? string.Empty,
        principal.FindAll(AuthClaims.Role).Select(c => c.Value).ToList(),
        principal.FindFirstValue(AuthClaims.Guest) == "true");

    /// <summary>The numeric users.id from the access token; never taken from the request body.</summary>
    public long GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(AuthClaims.Id);
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new InvalidCredentialsException("Access token is missing the user id");
    }
}
