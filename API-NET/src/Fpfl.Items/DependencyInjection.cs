using Fpfl.Items.Application.Services;
using Fpfl.Items.Domain.Repositories;
using Fpfl.Items.Domain.Services;
using Fpfl.Items.Infrastructure.Persistence;
using Fpfl.Items.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fpfl.Items;

/// <summary>
///     Registers the Items bounded context: its DbContext, repositories and services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFpflItems(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ItemsDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IItemTypeRepository, ItemTypeRepository>();
        services.AddScoped<IItemTypeService, ItemTypeService>();

        return services;
    }
}
