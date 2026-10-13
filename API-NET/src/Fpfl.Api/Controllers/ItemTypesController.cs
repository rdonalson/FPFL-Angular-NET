using Fpfl.Api.Dtos.ItemTypes;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Auth;
using Fpfl.Items.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     Item types (Credit, Debit, Initial Amount). Same routes and messages as API-JDK.
///     Reads are public; changes need ROLE_ADMIN because the ledger's balance math depends on
///     these ids (API-JDK leaves them open).
/// </summary>
[ApiController]
[Route("item-types")]
[Produces("application/json")]
[Tags("Item Types")]
public sealed class ItemTypesController(
    IItemTypeService service,
    ItemTypeMapper mapper,
    IApiResponseFactory responses) : ControllerBase
{
    /// <summary>Lists all item types.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ItemTypeResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemTypeResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var itemTypes = await service.ListAsync(cancellationToken);
        IReadOnlyList<ItemTypeResponse> body = itemTypes.Select(mapper.ToResponse).ToList();

        return Ok(responses.Success(body, "ItemTypes fetched successfully"));
    }

    /// <summary>Gets one item type by id.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ApiResponse<ItemTypeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ItemTypeResponse>>> Get(long id, CancellationToken cancellationToken)
    {
        var itemType = await service.GetAsync(id, cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(itemType), $"ItemType {id} retrieved successfully"));
    }

    /// <summary>Creates a item type with the given id and name. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = AuthClaims.AdminRole)]
    [ProducesResponseType<ApiResponse<ItemTypeResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ItemTypeResponse>>> Create(
        ItemTypeRequest request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(mapper.ToEntity(request), cancellationToken);
        var location = $"/item-types/{created.Id}";

        return Created(location, responses.Created(mapper.ToResponse(created), "ItemType created successfully", location));
    }

    /// <summary>Renames a item type and optionally changes its id (send a new <c>id</c> in the body). Admin only.</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = AuthClaims.AdminRole)]
    [ProducesResponseType<ApiResponse<ItemTypeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ItemTypeResponse>>> Update(
        long id, UpdateItemTypeRequest request, CancellationToken cancellationToken)
    {
        var updated = await service.UpdateAsync(id, request.Id, mapper.SanitizeName(request.Name), cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(updated), "ItemType updated successfully"));
    }

    /// <summary>Deletes a item type that no item uses. Admin only.</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = AuthClaims.AdminRole)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);

        return Ok(responses.Success($"ItemType {id} deleted successfully"));
    }
}
