using AugmentService.Api.Workflows;
using AugmentService.Infrastructure.Data;
using AugmentService.Infrastructure.ProductData;
using AugmentService.Infrastructure.WeatherData;
using Dapr.Client;
using Dapr.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AugmentService.IntegrationTests;

/// <summary>
/// Shared helpers for integration tests that use WebApplicationFactory.
/// </summary>
internal static class IntegrationTestHelpers
{
    /// <summary>
    /// Removes all EF Core DbContext-related registrations for the given context types.
    /// </summary>
    public static void RemoveDbContextDescriptors(
        IServiceCollection services, params Type[] contextTypes)
    {
        var toRemove = services.Where(d =>
            d.ServiceType.IsGenericType && (
                d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) ||
                d.ServiceType.Name.Contains("IDbContextPool") ||
                d.ServiceType.Name.Contains("IScopedDbContextLease")
            ) && d.ServiceType.GetGenericArguments()
                .Any(t => contextTypes.Contains(t))).ToList();

        toRemove.AddRange(services.Where(d =>
            contextTypes.Contains(d.ServiceType)).ToList());

        foreach (var d in toRemove)
            services.Remove(d);
    }

    /// <summary>
    /// Removes all Dapr-related hosted/background services and replaces
    /// application-level Dapr interfaces with lightweight no-op mocks.
    /// This prevents the Dapr actor runtime from trying to connect to a sidecar.
    /// </summary>
    public static void ReplaceDaprServices(IServiceCollection services)
    {
        var daprDescriptors = services
            .Where(d => d.ServiceType.FullName != null &&
                        (d.ServiceType.FullName.Contains("Dapr") ||
                         d.ServiceType.FullName.Contains("Workflow") ||
                         d.ImplementationType?.FullName?.Contains("Dapr") == true ||
                         d.ImplementationType?.FullName?.Contains("Workflow") == true))
            .ToList();

        foreach (var d in daprDescriptors)
            services.Remove(d);

        services.AddSingleton<DaprClient>(Mock.Of<DaprClient>());
        services.AddSingleton<IOrderWorkflowClient>(Mock.Of<IOrderWorkflowClient>());
        services.AddSingleton<IDaprWorkflowClient>(Mock.Of<IDaprWorkflowClient>());
    }

    /// <summary>
    /// Calls EnsureCreated on a DbContext to create the SQLite schema.
    /// Errors are silently swallowed since not every endpoint needs every DB.
    /// </summary>
    public static void InitDb<TContext>(IServiceProvider sp)
        where TContext : DbContext
    {
        try
        {
            var db = sp.GetRequiredService<TContext>();
            db.Database.EnsureCreated();
        }
        catch
        {
            // Ignore — the test may not require this database.
        }
    }
}
