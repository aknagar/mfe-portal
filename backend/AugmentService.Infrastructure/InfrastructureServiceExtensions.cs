using AugmentService.Application.Interfaces;
using AugmentService.Application.Services;
using AugmentService.Core.Interfaces;
using AugmentService.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AugmentService.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddSingleton<IProxyTargetRepository, InMemoryProxyTargetRepository>();

        // Register application services
        services.AddScoped<IProxyService, ProxyApplicationService>();

        // Configure HttpClient for proxy operations
        services.AddHttpClient<IProxyService, ProxyApplicationService>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
