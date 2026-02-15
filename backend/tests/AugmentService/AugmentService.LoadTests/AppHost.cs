using Aspire.Hosting;
using CommunityToolkit.Aspire.Hosting.k6;

namespace AugmentService.LoadTests;

/// <summary>
/// AppHost configuration for AugmentService load tests using k6
/// </summary>
public static class AppHost
{
    /// <summary>
    /// Creates a configured distributed application with k6 load testing
    /// </summary>
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Add the AugmentService API
        var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice-api");

        // Add k6 load testing
        var k6 = builder.AddK6("k6")
            .WithBindMount("scripts", "/scripts", isReadOnly: true)
            .WithScript("/scripts/main.js")
            .WithReference(augmentService)
            .WaitFor(augmentService);

        return builder;
    }
}
