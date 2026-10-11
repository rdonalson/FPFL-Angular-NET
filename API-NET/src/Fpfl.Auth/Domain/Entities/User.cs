namespace Fpfl.Auth.Domain.Entities;

/// <summary>Maps to <c>fpfl.users</c> (shared with API-JDK).</summary>
public sealed class User
{
    public long Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash ($2a$...), compatible with Spring's BCryptPasswordEncoder.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Set by the database.</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Public GUID that owns the user's items.</summary>
    public Guid UserId { get; set; }

    public string? First { get; set; }

    public string? Last { get; set; }
}
