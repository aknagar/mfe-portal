using Application;
using Application.Weather;
using AugmentService.Core.Interfaces;
using AugmentService.Infrastructure.Repositories;
using AugmentService.Infrastructure.WeatherData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AugmentService.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder
            .AddInfrastructureConfig();
        
        builder.Services.AddDbContext<WeatherDatabaseContext>();

        builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
        
        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WeatherDatabaseContext>());

        return builder;
    }
    
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(this IHealthChecksBuilder healthChecksBuilder)
    {
        healthChecksBuilder.AddDbContextCheck<WeatherDatabaseContext>();

        return healthChecksBuilder;
    }
}
