using Fpfl.Display.Application.Services;
using Fpfl.Display.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fpfl.Display;

/// <summary>Registers the Display bounded context. Needs <c>AddFpflItems</c> for the item repository.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFpflDisplay(this IServiceCollection services)
    {
        services.AddScoped<ILedgerReadoutService, LedgerReadoutService>();
        return services;
    }
}
