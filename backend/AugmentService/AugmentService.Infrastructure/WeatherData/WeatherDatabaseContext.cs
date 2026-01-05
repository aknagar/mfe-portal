using Application;
using AugmentService.Core;
using AugmentService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AugmentService.Infrastructure.WeatherData;

public class WeatherDatabaseContext(IOptions<InfrastructureConfig> config) : DbContext, IUnitOfWork
{
    public DbSet<Forecast> Forecasts { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(config.Value.ConnectionString)            
            .EnableSensitiveDataLogging(config.Value.EnableSensitiveDataLogging);
    }
}

public static class Extensions
{
    public static void CreateWeatherDbIfNotExists(this IHost host)
    {
        using var scope = host.Services.CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<WeatherDatabaseContext>();
        context.Database.EnsureCreated();
        DbInitializer.Initialize(context);
    }
}

public static class DbInitializer
{
    public static void Initialize(WeatherDatabaseContext context)
    {
        if (context.Forecasts.Any())
            return;

        var products = new List<Forecast>
        {
            //new Forecast { Id = Guid.NewGuid(), Date = new DateOnly(2025,01,01), TemperatureC = 25, Summary = "This is test Summary" },

        };

        context.AddRange(products);

        context.SaveChanges();
    }
}


