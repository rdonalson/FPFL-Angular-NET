using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Fpfl.Common.Sanitization;

namespace Fpfl.Api.Dtos.Logging;

/// <summary>
///     A log entry sent by the UI (same fields as API-JDK's ClientLogRequest).
///     Free text by nature, so it skips the strict sanitizer; the controller strips control
///     characters and truncates before anything reaches the log.
/// </summary>
public sealed record ClientLogRequest(
    [MaxLength(10)] string? Level,
    [MaxLength(2048)] string? Url,
    int? Status,
    [MaxLength(2000)] string? Message,
    [MaxLength(100)] string? CorrelationId,
    JsonElement? Details) : INoSanitize;
