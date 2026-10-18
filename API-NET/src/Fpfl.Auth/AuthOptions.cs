using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace Fpfl.Auth;

/// <summary>
///     Settings bound from the "Auth" configuration section.
///     <see cref="SigningKey" /> must come from user-secrets (dev) or an environment variable (prod),
///     never from appsettings.json or source code.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "fpfl-api-net";

    public string Audience { get; set; } = "fpfl-ui-ng";

    /// <summary>HMAC-SHA256 secret, at least 32 characters.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>Account used by the "Guest Login" button. Its password never leaves the server.</summary>
    public string GuestEmail { get; set; } = "guest.user@gmail.com";

    public SymmetricSecurityKey CreateSigningKey()
    {
        var bytes = Encoding.UTF8.GetBytes(SigningKey);
        if (bytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Auth:SigningKey is missing or shorter than 32 characters. " +
                "Set it with: dotnet user-secrets set \"Auth:SigningKey\" \"<random value>\" --project src/Fpfl.Api");
        }

        return new SymmetricSecurityKey(bytes);
    }
}
