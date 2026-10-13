using Fpfl.Api.Dtos.TimePeriods;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Auth;
using Fpfl.Items.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     Time periods (One Time, Daily, Weekly, Monthly, ...). Same routes and messages as API-JDK.
///     Reads are public like item types; changes need ROLE_ADMIN because the ledger's recurrence
///     rules depend on these ids (API-JDK leaves them open).
/// </summary>
[ApiController]
[Route("time-periods")]
[Produces("application/json")]
[Tags("Time Periods")]
public sealed class TimePeriodsController(
    ITimePeriodService service,
    TimePeriodMapper mapper,
    IApiResponseFactory responses) : ControllerBase
{
    /// <summary>Lists all time periods.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<TimePeriodResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TimePeriodResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var timePeriods = await service.ListAsync(cancellationToken);
        IReadOnlyList<TimePeriodResponse> body = timePeriods.Select(mapper.ToResponse).ToList();

        return Ok(responses.Success(body, "TimePeriods fetched successfully"));
    }

    /// <summary>Gets one time period by id.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ApiResponse<TimePeriodResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TimePeriodResponse>>> Get(long id, CancellationToken cancellationToken)
    {
        var timePeriod = await service.GetAsync(id, cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(timePeriod), $"TimePeriod {id} retrieved successfully"));
    }

    /// <summary>Creates a time period with the given id and name. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = AuthClaims.AdminRole)]
    [ProducesResponseType<ApiResponse<TimePeriodResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TimePeriodResponse>>> Create(
        TimePeriodRequest request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(mapper.ToEntity(request), cancellationToken);
        var location = $"/time-periods/{created.Id}";

        return Created(location, responses.Created(mapper.ToResponse(created), "TimePeriod created successfully", location));
    }

    /// <summary>Renames a time period. Admin only.</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = AuthClaims.AdminRole)]
    [ProducesResponseType<ApiResponse<TimePeriodResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TimePeriodResponse>>> Update(
        long id, UpdateTimePeriodNameRequest request, CancellationToken cancellationToken)
    {
        var updated = await service.UpdateNameAsync(id, mapper.SanitizeName(request.Name), cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(updated), "TimePeriod updated successfully"));
    }

    /// <summary>Deletes a time period that no item uses. Admin only.</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = AuthClaims.AdminRole)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);

        return Ok(responses.Success($"TimePeriod {id} deleted successfully"));
    }
}
