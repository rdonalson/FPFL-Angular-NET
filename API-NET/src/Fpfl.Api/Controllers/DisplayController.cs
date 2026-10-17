using Fpfl.Api.Dtos.Display;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Auth;
using Fpfl.Display.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     The forecast ledger behind the Display page's chart and table. Same route and response shape as
///     API-JDK's DisplayController, plus: a signed-in user is required, and a user can only build their own
///     ledger (API-JDK trusted the userId in the body). Ranges are capped at
///     <see cref="ILedgerReadoutService.MaxYears" /> years.
/// </summary>
[ApiController]
[Route("display")]
[Authorize]
[Produces("application/json")]
[Tags("Display")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
public sealed class DisplayController(
    ILedgerReadoutService ledger,
    DisplayMapper mapper,
    AuthMapper authMapper,
    IApiResponseFactory responses) : ControllerBase
{
    /// <summary>One row per day from ledgerStartDate to ledgerEndDate (inclusive), each with its credits, debits and running balance.</summary>
    [HttpPost("ledger")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<LedgerDayResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LedgerDayResponse>>>> BuildLedger(
        LedgerRequest request, CancellationToken cancellationToken)
    {
        var caller = authMapper.GetUserGuid(User);

        var days = await ledger.BuildAsync(
            request.UserId ?? caller,
            request.LedgerStartDate!.Value,
            request.LedgerEndDate!.Value,
            caller,
            User.IsInRole(AuthClaims.AdminRole),
            cancellationToken);

        return Ok(responses.Success(mapper.ToResponse(days), "Ledger retrieved successfully"));
    }
}
