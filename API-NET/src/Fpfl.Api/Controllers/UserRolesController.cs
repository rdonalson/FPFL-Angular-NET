using Fpfl.Api.Dtos.UserRoles;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Auth;
using Fpfl.Auth.Application.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     Manage a user's roles. Same routes and messages as API-JDK's UserRolesController,
///     restricted to ROLE_ADMIN (API-JDK only requires a signed-in user).
///     Changes reach the user's access token at its next refresh.
/// </summary>
[ApiController]
[Route("users/{userId:long}/roles")]
[Authorize(Roles = AuthClaims.AdminRole)]
[Produces("application/json")]
[Tags("User Roles")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
public sealed class UserRolesController(
    IUserRolesService service,
    AuthMapper authMapper,
    IApiResponseFactory responses) : ControllerBase
{
    /// <summary>Lists the user's role names.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<string>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> GetRoles(
        long userId, CancellationToken cancellationToken)
    {
        var roles = await service.GetRoleNamesAsync(userId, cancellationToken);

        return Ok(responses.Success(roles, "Roles retrieved successfully"));
    }

    /// <summary>Gives the user a role (no change if they already have it).</summary>
    [HttpPost("{roleId:long}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object?>>> AddRole(
        long userId, long roleId, CancellationToken cancellationToken)
    {
        await service.AddRoleAsync(userId, roleId, cancellationToken);

        return Ok(responses.Success("Role added successfully"));
    }

    /// <summary>Takes a role away from the user.</summary>
    [HttpDelete("{roleId:long}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object?>>> RemoveRole(
        long userId, long roleId, CancellationToken cancellationToken)
    {
        var removed = await service.RemoveRoleAsync(authMapper.GetUserId(User), userId, roleId, cancellationToken);

        return Ok(responses.Success(removed ? "Role removed successfully" : "Role was not assigned to user"));
    }

    /// <summary>Replaces all of the user's roles. An empty list clears them.</summary>
    [HttpPut]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object?>>> ReplaceRoles(
        long userId, ReplaceUserRolesRequest request, CancellationToken cancellationToken)
    {
        await service.ReplaceRolesAsync(authMapper.GetUserId(User), userId, request.RoleIds, cancellationToken);

        return Ok(responses.Success("Roles replaced successfully"));
    }
}
