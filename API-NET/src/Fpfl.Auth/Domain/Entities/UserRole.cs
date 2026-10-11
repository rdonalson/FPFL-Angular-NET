namespace Fpfl.Auth.Domain.Entities;

/// <summary>Maps to <c>fpfl.user_roles</c> (composite key user_id + role_id).</summary>
public sealed class UserRole
{
    public long UserId { get; set; }

    public long RoleId { get; set; }
}
