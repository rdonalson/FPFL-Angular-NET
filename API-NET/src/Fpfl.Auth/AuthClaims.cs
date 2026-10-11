namespace Fpfl.Auth;

/// <summary>
///     Claim names written into access tokens. Short JWT names are kept as-is
///     (JwtBearer is configured with MapInboundClaims = false).
/// </summary>
public static class AuthClaims
{
    /// <summary>Email address (same subject as API-JDK).</summary>
    public const string Subject = "sub";

    /// <summary>Numeric users.id.</summary>
    public const string Id = "id";

    /// <summary>users.user_id (GUID) that owns the user's financial data.</summary>
    public const string UserId = "user_id";

    public const string Role = "role";

    public const string Guest = "guest";

    public const string AdminRole = "ROLE_ADMIN";

    public const string UserRole = "ROLE_USER";
}
