using Fpfl.Items.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fpfl.Items.Infrastructure.Health;

/// <summary>
///     Reports whether the PostgreSQL database behind the Items context is reachable.
///     Shown as the "db" component on /health.
/// </summary>
public sealed class ItemsDatabaseHealthCheck(ItemsDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var details = new Dictionary<string, object>
        {
            ["database"] = "PostgreSQL",
            ["name"] = db.Database.GetDbConnection().Database
        };

        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database reachable", details)
                : HealthCheckResult.Unhealthy("Cannot connect to database", data: details);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex, details);
        }
    }
}
