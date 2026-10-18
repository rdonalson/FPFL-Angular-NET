using Fpfl.Common.Sanitization;

using Microsoft.Extensions.DependencyInjection;

namespace Fpfl.Common;

/// <summary>
///     Registers the shared-kernel services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFpflCommon(this IServiceCollection services)
    {
        services.AddSingleton<ISanitizer, Sanitizer>();
        return services;
    }
}
