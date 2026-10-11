namespace Fpfl.Auth.Domain.Entities;

/// <summary>Maps to <c>fpfl.roles</c>; names look like "ROLE_ADMIN".</summary>
public sealed class Role
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
