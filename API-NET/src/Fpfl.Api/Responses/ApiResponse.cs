namespace Fpfl.Api.Responses;

/// <summary>
///     Standard envelope for every API response, success or error. Same shape as API-JDK:
///     <c>{ status, message, data, correlationId, timestamp }</c>.
/// </summary>
/// <param name="Status">HTTP status code, repeated in the body.</param>
/// <param name="Message">Human-readable outcome.</param>
/// <param name="Data">Payload; null for errors and for operations that return nothing.</param>
/// <param name="CorrelationId">Error reference (matches the server log) or a Location for 201 responses.</param>
public sealed record ApiResponse<T>(int Status, string Message, T? Data = default, string CorrelationId = "")
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
