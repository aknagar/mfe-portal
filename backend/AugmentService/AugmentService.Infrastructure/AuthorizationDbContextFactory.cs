using AugmentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace AugmentService.Infrastructure;

/// <summary>
/// Design-time factory for creating AuthorizationDbContext instances.
/// Used by EF Core tools (migrations) when the application isn't running.
/// </summary>
public class AuthorizationDbContextFactory : IDesignTimeDbContextFactory<AuthorizationDbContext>
{
    public AuthorizationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthorizationDbContext>();
        
        // Use a default connection string for design-time operations
        // This will be overridden at runtime by the actual configuration
        optionsBuilder.UseNpgsql("Host=localhost;Database=mfeportal;Username=postgres;Password=postgres");

        // Create a mock IOptions<InfrastructureConfig>
        var config = Options.Create(new InfrastructureConfig
        {
            ConnectionString = "Host=localhost;Database=mfeportal;Username=postgres;Password=postgres",
            EnableSensitiveDataLogging = false
        });

        return new AuthorizationDbContext(optionsBuilder.Options, config);
    }
}
