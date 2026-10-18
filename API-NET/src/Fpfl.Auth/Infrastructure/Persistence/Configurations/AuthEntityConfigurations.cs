using Fpfl.Auth.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fpfl.Auth.Infrastructure.Persistence.Configurations;

// Column names match the tables Hibernate created for API-JDK (snake_case).

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", AuthDbContext.Schema);
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(u => u.Email).HasColumnName("email").IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
        builder.Property(u => u.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(u => u.First).HasColumnName("first").HasMaxLength(50);
        builder.Property(u => u.Last).HasColumnName("last").HasMaxLength(100);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", AuthDbContext.Schema);
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(r => r.Name).HasColumnName("name").IsRequired();
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", AuthDbContext.Schema);
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.Property(ur => ur.UserId).HasColumnName("user_id");
        builder.Property(ur => ur.RoleId).HasColumnName("role_id");
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", AuthDbContext.Schema);
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(t => t.Token).HasColumnName("token").IsRequired();
        builder.Property(t => t.UserId).HasColumnName("user_id");
        builder.Property(t => t.ExpiryDate).HasColumnName("expiry_date");

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId);
    }
}
