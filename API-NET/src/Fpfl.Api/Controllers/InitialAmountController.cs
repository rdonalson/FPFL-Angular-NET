using Fpfl.Api.Dtos.InitialAmounts;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Auth;
using Fpfl.Items.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     The signed-in user's starting balance. Same routes and messages as API-JDK, plus: every call
///     needs a signed-in user, users can only reach their own amount (admins can reach all), and the
///     GET creates a $0.00 amount when the user doesn't have one yet, so it never returns 404.
/// </summary>
[ApiController]
[Route("initial-amount")]
[Authorize]
[Produces("application/json")]
[Tags("Initial Amount")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
public sealed class InitialAmountController(
    IInitialAmountService service,
    ItemMapper mapper,
    AuthMapper authMapper,
    IApiResponseFactory responses) : ControllerBase
{
    /// <summary>Gets a user's initial amount, creating it at $0.00 first if needed. The user must be the caller (or the caller an admin).</summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType<ApiResponse<InitialAmountResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InitialAmountResponse>>> Get(Guid userId, CancellationToken cancellationToken)
    {
        var item = await service.GetOrCreateAsync(userId, authMapper.GetUserGuid(User), IsAdmin, cancellationToken);

        return Ok(responses.Success(mapper.ToInitialAmountResponse(item), "InitialAmount retrieved successfully"));
    }

    /// <summary>Creates the caller's initial amount (rarely needed: one is created automatically).</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<InitialAmountResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<InitialAmountResponse>>> Create(
        InitialAmountRequest request, CancellationToken cancellationToken)
    {
        var userId = authMapper.GetUserGuid(User);
        var item = await service.CreateAsync(request.Amount, userId, cancellationToken);
        var location = $"/initial-amount/{userId}";

        return Created(location, responses.Created(mapper.ToInitialAmountResponse(item), "InitialAmount created successfully", location));
    }

    /// <summary>Changes the caller's initial amount.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<ApiResponse<InitialAmountResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InitialAmountResponse>>> Update(
        long id, InitialAmountRequest request, CancellationToken cancellationToken)
    {
        var item = await service.UpdateAsync(id, request.Amount, authMapper.GetUserGuid(User), IsAdmin, cancellationToken);

        return Ok(responses.Success(mapper.ToInitialAmountResponse(item), "InitialAmount updated successfully"));
    }

    private bool IsAdmin => User.IsInRole(AuthClaims.AdminRole);
}
