using AugmentService.Application.Interfaces;
using AugmentService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Application;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMediatR(cfg =>
            cfg
                .RegisterServicesFromAssemblyContaining(typeof(DependencyInjection))
                .AddOpenBehavior(typeof(LoggingBehavior<,>))
        );

        // Register authorization service
        builder.Services.AddScoped<IPermissionService, AuthorizationService>();

        // Register memory cache if not already registered
        builder.Services.AddMemoryCache();

        return builder;
    }
}