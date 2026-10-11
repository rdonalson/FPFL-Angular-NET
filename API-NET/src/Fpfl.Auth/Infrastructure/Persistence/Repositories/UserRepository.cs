using Fpfl.Auth.Domain.Entities;
using Fpfl.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fpfl.Auth.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AuthDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

    public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(long userId, CancellationToken cancellationToken) =>
        await (from userRole in db.UserRoles
               join role in db.Roles on userRole.RoleId equals role.Id
               where userRole.UserId == userId
               orderby role.Name
               select role.Name)
            .ToListAsync(cancellationToken);

    public async Task<User> AddWithRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role is null)
        {
            role = new Role { Name = roleName };
            db.Roles.Add(role);
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken); // assigns user.Id and role.Id

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return user;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
