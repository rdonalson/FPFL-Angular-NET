using Fpfl.Auth.Domain.Entities;

namespace Fpfl.Auth.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetRoleNamesAsync(long userId, CancellationToken cancellationToken);

    /// <summary>Creates the user and gives them the named role (creating the role if needed).</summary>
    Task<User> AddWithRoleAsync(User user, string roleName, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
