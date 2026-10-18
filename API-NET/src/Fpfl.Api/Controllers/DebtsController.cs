using Fpfl.Api.Dtos.Debts;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Auth;
using Fpfl.Items.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     Debt Paydown: the debits a user is paying down and the credits allocated to them. Not part of
///     API-JDK. Signed-in users only; each user sees and changes only their own debts.
/// </summary>
[ApiController]
[Route("debts")]
[Authorize]
[Produces("application/json")]
[Tags("Debt Paydown")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
public sealed class DebtsController(
    IDebtService service,
    DebtMapper mapper,
    AuthMapper authMapper,
    IApiResponseFactory responses) : ControllerBase
{
    /// <summary>Lists the caller's tracked debts with their allocations.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<DebtResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DebtResponse>>>> List(CancellationToken cancellationToken)
    {
        var debts = await service.ListAsync(authMapper.GetUserGuid(User), cancellationToken);
        IReadOnlyList<DebtResponse> body = debts.Select(mapper.ToResponse).ToList();

        return Ok(responses.Success(body, "Debts fetched successfully"));
    }

    /// <summary>Gets one of the caller's tracked debts.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ApiResponse<DebtResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DebtResponse>>> Get(long id, CancellationToken cancellationToken)
    {
        var debt = await service.GetAsync(id, authMapper.GetUserGuid(User), IsAdmin, cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(debt), $"Debt {id} retrieved successfully"));
    }

    /// <summary>Starts tracking one of the caller's debits, with any allocations.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<DebtResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DebtResponse>>> Create(DebtRequest request, CancellationToken cancellationToken)
    {
        var debt = await service.CreateAsync(mapper.ToDraft(request), authMapper.GetUserGuid(User), cancellationToken);
        var location = $"/debts/{debt.Id}";

        return Created(location, responses.Created(mapper.ToResponse(debt), "Debt created successfully", location));
    }

    /// <summary>Changes a debt's balance, APR, balance date and allocations.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<ApiResponse<DebtResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DebtResponse>>> Update(
        long id, DebtRequest request, CancellationToken cancellationToken)
    {
        var debt = await service.UpdateAsync(id, mapper.ToDraft(request), authMapper.GetUserGuid(User), IsAdmin, cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(debt), "Debt updated successfully"));
    }

    /// <summary>Stops tracking a debt (the debit itself stays).</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, authMapper.GetUserGuid(User), IsAdmin, cancellationToken);

        return Ok(responses.Success("Debt deleted successfully"));
    }

    private bool IsAdmin => User.IsInRole(AuthClaims.AdminRole);
}
