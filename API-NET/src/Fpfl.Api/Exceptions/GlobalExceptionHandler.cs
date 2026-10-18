using Fpfl.Api.Responses;
using Fpfl.Common.Exceptions;

using Microsoft.AspNetCore.Diagnostics;

namespace Fpfl.Api.Exceptions;

/// <summary>
///     Port of API-JDK's GlobalExceptionHandler (@RestControllerAdvice). Maps domain exceptions
///     to HTTP statuses and returns the standard <see cref="ApiResponse{T}" /> body with a
///     correlation ID that matches the server log entry.
///     Difference from the Java version: unexpected errors (500) return a generic message,
///     never the raw exception text, so SQL or stack details do not leak to clients.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const string GenericErrorMessage =
        "An unexpected error occurred. Quote the correlation ID when reporting this issue.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Client disconnected; there is no one to respond to.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (status, message) = exception switch
        {
            SanitizationException or DomainValidationException => (StatusCodes.Status400BadRequest, exception.Message),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, exception.Message),
            ForbiddenOperationException => (StatusCodes.Status403Forbidden, exception.Message),
            ItemNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            DuplicateItemException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, GenericErrorMessage)
        };

        var correlationId = Guid.NewGuid().ToString();

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Correlation ID: {CorrelationId} - {Message}", correlationId, exception.Message);
        }
        else
        {
            logger.LogWarning("Correlation ID: {CorrelationId} - {ExceptionType}: {Message}",
                correlationId, exception.GetType().Name, exception.Message);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiResponse<object?>(status, message, null, correlationId),
            cancellationToken);

        return true;
    }
}
