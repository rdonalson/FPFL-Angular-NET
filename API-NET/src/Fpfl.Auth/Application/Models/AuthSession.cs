using Fpfl.Auth.Domain.Entities;

namespace Fpfl.Auth.Application.Models;

/// <summary>Everything the API returns after a successful login, guest login, register or refresh.</summary>
public sealed record AuthSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    User User,
    IReadOnlyList<string> Roles,
    bool IsGuest);
