using Aspire.Hosting;
using CommunityToolkit.Aspire.Hosting.k6;

namespace AugmentService.LoadTests;

/// <summary>
/// AppHost configuration for AugmentService load tests using k6
/// This AppHost is designed to be used with DistributedApplicationTestingBuilder
/// </summary>
public class AppHost
{
    public static async Task Main(string[] args)
    {
        var builder = CreateBuilder(args);
        await builder.Build().RunAsync();
    }

    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var options = new DistributedApplicationOptions { Args = args };
        return CreateBuilder(options);
    }

    public static IDistributedApplicationBuilder CreateBuilder(DistributedApplicationOptions options)
    {
        var builder = DistributedApplication.CreateBuilder(options);

        // Add the AugmentService API
        var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice-api");

        // Read script name from environment variable (set by tests)
        var scriptName = Environment.GetEnvironmentVariable("K6_SCRIPT_NAME")
                        ?? builder.Configuration["K6_SCRIPT_NAME"];

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
