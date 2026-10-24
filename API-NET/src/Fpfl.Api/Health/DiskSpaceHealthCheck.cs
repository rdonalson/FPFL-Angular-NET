using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fpfl.Api.Health;

/// <summary>
///     Reports free space on the drive the API runs from, like Spring Boot Actuator's
///     <c>diskSpace</c> check: DOWN once free space falls below the threshold (10 MB by default).
///     Shown as the "diskSpace" component on /health.
/// </summary>
public sealed class DiskSpaceHealthCheck(string? path = null, long thresholdBytes = DiskSpaceHealthCheck.DefaultThresholdBytes)
    : IHealthCheck
{
    /// <summary>Spring Boot's default: 10 MB.</summary>
    public const long DefaultThresholdBytes = 10L * 1024 * 1024;

    private readonly string _path = path ?? AppContext.BaseDirectory;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_path)) ?? _path);
            var details = new Dictionary<string, object>
            {
                ["total"] = drive.TotalSize,
                ["free"] = drive.AvailableFreeSpace,
                ["threshold"] = thresholdBytes,
                ["path"] = _path,
                ["exists"] = Directory.Exists(_path)
            };

            return Task.FromResult(drive.AvailableFreeSpace >= thresholdBytes
                ? HealthCheckResult.Healthy("Free space above threshold", details)
                : HealthCheckResult.Unhealthy("Free space below threshold", data: details));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Disk space check failed", ex,
                new Dictionary<string, object> { ["path"] = _path, ["threshold"] = thresholdBytes }));
        }
    }
}
