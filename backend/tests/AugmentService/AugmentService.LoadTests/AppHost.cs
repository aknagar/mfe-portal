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
    /// <param name="args">Command line arguments</param>
    /// <param name="scriptName">Optional specific script to run (e.g., "smoke-test.js"). If null, no K6 resources are added.</param>
    public static IDistributedApplicationBuilder CreateBuilder(string[] args, string? scriptName = null)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Add the AugmentService API
        var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice-api");

        // Add k6 load testing if script is specified
        if (!string.IsNullOrEmpty(scriptName))
        {
            var k6ResourceName = $"k6-{Path.GetFileNameWithoutExtension(scriptName)}";
            var k6 = builder.AddK6(k6ResourceName)
                .WithBindMount("scripts", "/scripts", isReadOnly: true)
                .WithScript($"/scripts/{scriptName}")
                .WithReference(augmentService)
                .WaitFor(augmentService);
        }

        return builder;
    }
}
