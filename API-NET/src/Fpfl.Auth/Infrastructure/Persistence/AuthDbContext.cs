using Fpfl.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fpfl.Auth.Infrastructure.Persistence;

/// <summary>
///     EF Core context for the Auth bounded context, mapped onto the existing <c>fpfl</c> tables
///     shared with API-JDK. Like ItemsDbContext, it must not be used to generate migrations
///     against that database.
/// </summary>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public const string Schema = "fpfl";

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
