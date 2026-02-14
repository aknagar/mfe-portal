using Application;
using AugmentService.Core;
using AugmentService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AugmentService.Infrastructure.WeatherData;

public class WeatherDatabaseContext(DbContextOptions<WeatherDatabaseContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Forecast> Forecasts { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Forecast entity
        modelBuilder.Entity<Forecast>(entity =>
        {
            entity.HasKey(f => f.Id);
            
            entity.Property(f => f.Date)
                .IsRequired();
            
            entity.Property(f => f.TemperatureC)
                .IsRequired();
            
            entity.Property(f => f.Summary)
                .HasMaxLength(500);
            
            entity.Property(f => f.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
        });
    }
}

public static class Extensions
{
    public static void CreateWeatherDbIfNotExists(this IHost host)
    {
        using var scope = host.Services.CreateScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<WeatherDatabaseContext>();
        
        // For in-memory databases (like SQLite :memory:), ensure the connection is open
        // This is important for test environments
        if (context.Database.IsSqlite() && context.Database.GetConnectionString()?.Contains("memory", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Database.OpenConnection();
        }
        
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


