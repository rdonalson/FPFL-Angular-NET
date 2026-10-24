using System.Reflection;
using System.Runtime.InteropServices;

namespace Fpfl.Api.Health;

/// <summary>
///     Body of <c>GET /info</c>, shaped like Spring Boot Actuator's /actuator/info so the UIs'
///     status pages can read either API: <c>app</c> and <c>build</c> sections, plus the runtime
///     the API is running on.
/// </summary>
public sealed record AppInfo(AppInfo.AppSection App, AppInfo.BuildSection Build, AppInfo.RuntimeSection Runtime)
{
    public sealed record AppSection(string Name, string Description, string Version);

    public sealed record BuildSection(string Name, string Artifact, string Version, string? Commit, DateTimeOffset? Time);

    public sealed record RuntimeSection(
        string Framework,
        string Os,
        string Environment,
        DateTimeOffset StartedAt,
        long UptimeSeconds);

    /// <summary>
    ///     Builds the info for the running API. <paramref name="assembly" /> supplies the version
    ///     ("1.0.0+3f2a9c1" is split into version and commit) and the build time.
    /// </summary>
    public static AppInfo Create(Assembly assembly, string environmentName, DateTimeOffset startedAt, DateTimeOffset now)
    {
        var (version, commit) = SplitVersion(
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0");

        return new AppInfo(
            new AppSection("FPFL API", "Financial Planner Forecast Ledger — .NET API", version),
            new BuildSection(assembly.GetName().Name ?? "Fpfl.Api", "api-net", version, commit, BuildTime(assembly)),
            new RuntimeSection(
                RuntimeInformation.FrameworkDescription,
                RuntimeInformation.OSDescription,
                environmentName,
                startedAt,
                (long)Math.Max(0, (now - startedAt).TotalSeconds)));
    }

    /// <summary>"1.0.0+3f2a9c1e…" → ("1.0.0", "3f2a9c1"); "1.0.0" → ("1.0.0", null).</summary>
    public static (string Version, string? Commit) SplitVersion(string informationalVersion)
    {
        var plus = informationalVersion.IndexOf('+');
        if (plus < 0)
        {
            return (informationalVersion, null);
        }

        var commit = informationalVersion[(plus + 1)..];
        return (informationalVersion[..plus], commit.Length > 7 ? commit[..7] : commit);
    }

    private static DateTimeOffset? BuildTime(Assembly assembly)
    {
        try
        {
            return string.IsNullOrEmpty(assembly.Location)
                ? null
                : new DateTimeOffset(File.GetLastWriteTimeUtc(assembly.Location), TimeSpan.Zero);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
