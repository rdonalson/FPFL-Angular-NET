using System.ComponentModel.DataAnnotations;

namespace Fpfl.Api.Dtos.UserRoles;

/// <summary>Body of PUT /users/{userId}/roles: <c>{ "roleIds": [1, 2] }</c>. An empty list clears all roles.</summary>
public sealed record ReplaceUserRolesRequest(
    [Required(ErrorMessage = "roleIds is required; send an empty list to clear roles")]
    IReadOnlyList<long> RoleIds);
