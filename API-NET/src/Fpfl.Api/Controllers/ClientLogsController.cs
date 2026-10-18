using Fpfl.Api.Dtos.Logging;
using Fpfl.Api.Responses;
using Fpfl.Common.Sanitization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fpfl.Api.Controllers;

/// <summary>
///     Receives log entries from the UI and writes them to the server log (port of API-JDK's
///     ClientLogController). Open to anonymous callers like the Java version, so it is
///     rate-limited per client IP, size-limited, and every field is cleaned before logging.
/// </summary>
[ApiController]
[Route("client-logs")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicy)]
[RequestSizeLimit(MaxBodyBytes)]
[Produces("application/json")]
[Tags("Client Logs")]
public sealed class ClientLogsController(
    ILogger<ClientLogsController> logger,
    IApiResponseFactory responses) : ControllerBase
{
    public const string RateLimitPolicy = "client-logs";
    private const int MaxBodyBytes = 16 * 1024;
    private const int MaxDetailsLength = 4000;

    /// <summary>Logs one client-side event at level info, warn or error.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
    public ActionResult<ApiResponse<object?>> Receive(ClientLogRequest request)
    {
        var correlationId = Clean(request.CorrelationId);
        var message = Clean(request.Message);

        switch (Clean(request.Level).ToLowerInvariant())
        {
            case "error":
                logger.LogError(
                    "[CLIENT LOG] [{CorrelationId}] {Message} | URL: {Url} | Status: {Status} | Details: {Details}",
                    correlationId, message, Clean(request.Url), request.Status,
                    Clean(request.Details?.GetRawText(), MaxDetailsLength));
                break;
            case "warn":
                logger.LogWarning("[CLIENT LOG] [{CorrelationId}] {Message}", correlationId, message);
                break;
            default:
                logger.LogInformation("[CLIENT LOG] [{CorrelationId}] {Message}", correlationId, message);
                break;
        }

        return Ok(responses.Success("Client log received"));
    }

    /// <summary>Removes control characters (no forged log lines) and caps the length.</summary>
    private static string Clean(string? value, int maxLength = 2048)
    {
        var normalized = Sanitizer.Normalize(value) ?? string.Empty;
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "…";
    }
}
