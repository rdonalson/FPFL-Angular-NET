namespace Fpfl.Auth.Domain.Entities;

/// <summary>
///     Maps to <c>fpfl.refresh_tokens</c>. API-NET stores a SHA-256 hash in <see cref="Token" />,
///     never the raw value, and deletes the row when the token is used (rotation) or on logout.
/// </summary>
public sealed class RefreshToken
{
    public long Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime ExpiryDate { get; set; }
}
