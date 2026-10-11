using Fpfl.Common.Sanitization;

namespace Fpfl.Api.Dtos.Auth;

/// <summary>
///     Session returned by login, guest login, register, refresh and change-password.
///     Same fields as API-JDK's AuthResponse, plus <c>accessTokenExpiresAt</c> (so the UI doesn't
///     have to decode the JWT) and <c>isGuest</c>. Skips the sanitizer: emails and tokens contain
///     characters that strict text rules reject.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    long Id,
    string Email,
    Guid UserId,
    string? First,
    string? Last,
    IReadOnlyList<string> Roles,
    bool IsGuest) : INoSanitize;

/// <summary>Who the current access token belongs to (GET /auth/me).</summary>
public sealed record CurrentUserResponse(
    long Id,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsGuest) : INoSanitize;
