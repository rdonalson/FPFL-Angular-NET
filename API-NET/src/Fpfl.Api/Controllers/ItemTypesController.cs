using Fpfl.Api.Dtos.ItemTypes;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Items.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     Read endpoints for item types (Credit, Debit, Initial Amount). Same routes as API-JDK.
///     Errors are not handled here; domain exceptions flow to <c>GlobalExceptionHandler</c>.
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
}
