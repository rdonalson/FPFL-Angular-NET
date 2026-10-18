using Fpfl.Api.Dtos.Items;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Auth;
using Fpfl.Items.Domain.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     A user's credits and debits. Same routes and messages as API-JDK, but every call needs a signed-in
///     user, users can only read or change their own items (admins can reach all), the owner always
///     comes from the access token, and saves are validated by ItemRules.
/// </summary>
[ApiController]
[Route("items")]
[Authorize]
[Produces("application/json")]
[Tags("Items")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
public sealed class ItemsController(
    IItemService service,
    ItemMapper mapper,
    AuthMapper authMapper,
    IApiResponseFactory responses) : ControllerBase
{
    /// <summary>Lists every user's items. Admin only.</summary>
    [HttpGet]
    [Authorize(Roles = AuthClaims.AdminRole)]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ItemResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemResponse>>>> List(CancellationToken cancellationToken)
    {
        var items = await service.ListAllAsync(cancellationToken);
        IReadOnlyList<ItemResponse> body = items.Select(mapper.ToResponse).ToList();

        return Ok(responses.Success(body, "Items fetched successfully"));
    }

    /// <summary>Gets one of the caller's items by id.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ApiResponse<ItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ItemResponse>>> Get(long id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, authMapper.GetUserGuid(User), IsAdmin, cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(item), $"Item {id} retrieved successfully"));
    }

    /// <summary>Lists a user's items of one type: 1 = credits, 2 = debits. The user must be the caller (or the caller an admin).</summary>
    [HttpGet("{userId:guid}/{itemType:long}")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ItemResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemResponse>>>> ListForUser(
        Guid userId, long itemType, CancellationToken cancellationToken)
    {
        var items = await service.ListForUserAsync(
            userId, itemType, authMapper.GetUserGuid(User), IsAdmin, cancellationToken);
        IReadOnlyList<ItemResponse> body = items.Select(mapper.ToResponse).ToList();

        return Ok(responses.Success(body, "Items retrieved successfully"));
    }

    /// <summary>Creates a credit or debit owned by the caller.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<ItemResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ItemResponse>>> Create(
        ItemRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(mapper.ToDraft(request), authMapper.GetUserGuid(User), cancellationToken);
        var location = $"/items/{item.Id}";

        return Created(location, responses.Created(mapper.ToResponse(item), "Item created successfully", location));
    }

    /// <summary>Replaces one of the caller's items (including switching its time period).</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<ApiResponse<ItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ItemResponse>>> Update(
        long id, ItemRequest request, CancellationToken cancellationToken)
    {
        var item = await service.UpdateAsync(
            id, mapper.ToDraft(request), authMapper.GetUserGuid(User), IsAdmin, cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(item), "Item updated successfully"));
    }

    /// <summary>Deletes one of the caller's items.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, authMapper.GetUserGuid(User), IsAdmin, cancellationToken);

        return Ok(responses.Success($"Item {id} deleted successfully"));
    }

    private bool IsAdmin => User.IsInRole(AuthClaims.AdminRole);
}
