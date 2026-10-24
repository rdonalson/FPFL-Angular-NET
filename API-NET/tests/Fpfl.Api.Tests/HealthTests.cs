using System.Text.Json;

using Fpfl.Api.Health;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fpfl.Api.Tests;

/// <summary>/health's Actuator-style body, the diskSpace check, and /info.</summary>
public sealed class HealthTests
{
    private static HealthCheckContext Context() => new()
    {
        Registration = new HealthCheckRegistration("diskSpace", new DiskSpaceHealthCheck(), null, null)
    };

    [Fact]
    public async Task DiskSpace_is_up_while_free_space_is_above_the_threshold()
    {
        var result = await new DiskSpaceHealthCheck(AppContext.BaseDirectory, thresholdBytes: 0)
            .CheckHealthAsync(Context());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.True((long)result.Data["total"] > 0);
        Assert.True((long)result.Data["free"] >= 0);
        Assert.Equal(0L, result.Data["threshold"]);
        Assert.Equal(true, result.Data["exists"]);
    }

    [Fact]
    public async Task DiskSpace_is_down_below_the_threshold()
    {
        var result = await new DiskSpaceHealthCheck(AppContext.BaseDirectory, thresholdBytes: long.MaxValue)
            .CheckHealthAsync(Context());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Free space below threshold", result.Description);
    }

    [Fact]
    public void DiskSpace_defaults_to_Spring_Boots_10_MB_threshold()
    {
        Assert.Equal(10L * 1024 * 1024, DiskSpaceHealthCheck.DefaultThresholdBytes);
    }

    [Fact]
    public async Task Health_is_written_in_Actuator_shape()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["db"] = new(HealthStatus.Healthy, "Database reachable", TimeSpan.FromMilliseconds(12.34), null,
                    new Dictionary<string, object> { ["database"] = "PostgreSQL" }),
                ["diskSpace"] = new(HealthStatus.Degraded, null, TimeSpan.Zero, null, null),
                ["ping"] = new(HealthStatus.Unhealthy, "down", TimeSpan.Zero, null, null)
            },
            TimeSpan.FromMilliseconds(20));

        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        context.Response.Body = new MemoryStream();

        await HealthResponseWriter.WriteAsync(context, report);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        var root = json.RootElement;
        Assert.Equal("DOWN", root.GetProperty("status").GetString());

        var components = root.GetProperty("components");
        var db = components.GetProperty("db");
        Assert.Equal("UP", db.GetProperty("status").GetString());
        Assert.Equal("Database reachable", db.GetProperty("description").GetString());
        Assert.Equal(12.3, db.GetProperty("durationMs").GetDouble());
        Assert.Equal("PostgreSQL", db.GetProperty("details").GetProperty("database").GetString());
        Assert.Equal("DEGRADED", components.GetProperty("diskSpace").GetProperty("status").GetString());
        Assert.Equal("DOWN", components.GetProperty("ping").GetProperty("status").GetString());
    }

    [Fact]
    public void Info_names_the_app_and_its_runtime()
    {
        var started = new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

        var info = AppInfo.Create(typeof(HealthResponseWriter).Assembly, "Production", started, started.AddMinutes(90));

        Assert.Equal("FPFL API", info.App.Name);
        Assert.False(string.IsNullOrWhiteSpace(info.App.Version));
        Assert.Equal(info.App.Version, info.Build.Version);
        Assert.Equal("Fpfl.Api", info.Build.Name);
        Assert.Equal("api-net", info.Build.Artifact);
        Assert.NotNull(info.Build.Time);
        Assert.Equal("Production", info.Runtime.Environment);
        Assert.Contains(".NET", info.Runtime.Framework);
        Assert.Equal(started, info.Runtime.StartedAt);
        Assert.Equal(5400, info.Runtime.UptimeSeconds);
    }

    [Fact]
    public void Info_never_reports_negative_uptime()
    {
        var started = DateTimeOffset.UtcNow;
        Assert.Equal(0, AppInfo.Create(typeof(HealthResponseWriter).Assembly, "Development", started, started.AddSeconds(-5))
            .Runtime.UptimeSeconds);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0", null)]
    [InlineData("1.0.0+3f2a9c1e5b7d", "1.0.0", "3f2a9c1")]
    [InlineData("2.1.0-beta+abc", "2.1.0-beta", "abc")]
    public void Splits_the_informational_version_into_version_and_commit(string raw, string version, string? commit)
    {
        Assert.Equal((version, commit), AppInfo.SplitVersion(raw));
    }
}
