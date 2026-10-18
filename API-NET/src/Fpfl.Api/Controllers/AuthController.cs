using Fpfl.Api.Dtos.Auth;
using Fpfl.Api.Mappers;
using Fpfl.Api.Responses;
using Fpfl.Auth.Application.Services;
using Fpfl.Items.Domain.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fpfl.Api.Controllers;

/// <summary>
///     Login, guest login, register, token refresh, logout and change password.
///     Routes match API-JDK (/auth/...), plus /auth/guest, /auth/logout and /auth/me.
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
[Tags("Auth")]
public sealed class AuthController(
    IAuthService authService,
    IInitialAmountService initialAmounts,
    AuthMapper mapper,
    IApiResponseFactory responses,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>Signs in with email and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        return Ok(responses.Success(mapper.ToResponse(session), "Login successful"));
    }

    /// <summary>Signs in as the shared guest (demo) account. No password is sent.</summary>
    [HttpPost("guest")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Guest(CancellationToken cancellationToken)
    {
        var session = await authService.GuestLoginAsync(cancellationToken);
        return Ok(responses.Success(mapper.ToResponse(session), "Guest session started"));
    }

    /// <summary>Creates an account (role ROLE_USER) with a $0.00 initial amount, and signs it in.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.RegisterAsync(
            request.Email, request.Password, request.First, request.Last, cancellationToken);

        // Give every new user a starting balance (as the original version did). The account already
        // exists, so a failure here must not fail the registration; the Initial Amount page creates
        // the amount on first visit anyway.
        try
        {
            await initialAmounts.EnsureAsync(session.User.UserId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not create the initial amount for new user {UserId}", session.User.UserId);
        }

        return Ok(responses.Success(mapper.ToResponse(session), "Registration successful"));
    }

    /// <summary>Exchanges a refresh token for a new session. Each refresh token works once.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        RefreshRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        return Ok(responses.Success(mapper.ToResponse(session), "Token refreshed"));
    }

    /// <summary>Revokes the refresh token. Works even after the access token has expired.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object?>>> Logout(
        LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return Ok(responses.Success("Logged out"));
    }

    /// <summary>Changes the signed-in user's password. Not allowed for the guest account.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.ChangePasswordAsync(
            mapper.GetUserId(User), request.CurrentPassword, request.NewPassword, cancellationToken);
        return Ok(responses.Success(mapper.ToResponse(session), "Password changed successfully"));
    }

    /// <summary>Returns who the current access token belongs to.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<ApiResponse<CurrentUserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<CurrentUserResponse>> Me() =>
        Ok(responses.Success(mapper.ToCurrentUser(User), "Current user"));
}
