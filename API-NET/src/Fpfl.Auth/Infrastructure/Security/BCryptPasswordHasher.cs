using Fpfl.Auth.Application.Security;

namespace Fpfl.Auth.Infrastructure.Security;

/// <summary>
///     BCrypt, compatible with the $2a$10$ hashes Spring's BCryptPasswordEncoder writes,
///     so accounts created through API-JDK can sign in here and vice versa.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 10; // Spring's default strength

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception)
        {
            return false; // malformed or unsupported hash in the database
        }
    }
}
