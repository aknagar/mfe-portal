using Aspire.Hosting;
using CommunityToolkit.Aspire.Hosting.k6;
using Microsoft.Extensions.Configuration;

namespace AugmentService.LoadTests;

/// <summary>
/// AppHost configuration for AugmentService load tests using k6
/// This AppHost is designed to be used with DistributedApplicationTestingBuilder
/// </summary>
public class AppHost
{
    public static IDistributedApplicationBuilder CreateBuilder(DistributedApplicationOptions options)
    {
        var builder = DistributedApplication.CreateBuilder(options);

        // Add the AugmentService API
        var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice-api");

        // Read script name from configuration (set by tests)
        var scriptName = builder.Configuration["TestScriptName"];

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
