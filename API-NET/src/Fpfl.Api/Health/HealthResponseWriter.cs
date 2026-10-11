using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fpfl.Api.Health;

/// <summary>
///     Writes /health in the same shape as Spring Boot Actuator, so the landing page and
///     the UI's status page can treat both APIs alike:
///     <c>{ "status": "UP", "components": { "db": { "status": "UP", ... } } }</c>.
/// </summary>
public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var body = new
        {
            status = ToActuatorStatus(report.Status),
            components = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = ToActuatorStatus(entry.Value.Status),
                    description = entry.Value.Description,
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                    details = entry.Value.Data
                })
        };

        return context.Response.WriteAsJsonAsync(body);
    }

    private static string ToActuatorStatus(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "UP",
        HealthStatus.Degraded => "DEGRADED",
        _ => "DOWN"
    };
}
