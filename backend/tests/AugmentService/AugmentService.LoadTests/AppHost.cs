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
        var scriptName = Environment.GetEnvironmentVariable("K6_SCRIPT_NAME") ?? "main.js";

        // Get the absolute path to the scripts directory
        var scriptsPath = Path.Combine(AppContext.BaseDirectory, "scripts");

        if (!Directory.Exists(scriptsPath))
        {
            // Try to find scripts directory relative to the project
            var projectDir = Directory.GetCurrentDirectory();
            scriptsPath = Path.Combine(projectDir, "scripts");
        }

        Console.WriteLine($"[K6 AppHost] Script name: {scriptName}");
        Console.WriteLine($"[K6 AppHost] Scripts path: {scriptsPath}");
        Console.WriteLine($"[K6 AppHost] Directory exists: {Directory.Exists(scriptsPath)}");
        Console.WriteLine($"[K6 AppHost] AppContext.BaseDirectory: {AppContext.BaseDirectory}");
        Console.WriteLine($"[K6 AppHost] Current directory: {Directory.GetCurrentDirectory()}");

        if (Directory.Exists(scriptsPath))
        {
            var scriptFiles = Directory.GetFiles(scriptsPath, "*.js");
            Console.WriteLine($"[K6 AppHost] Scripts found: {string.Join(", ", scriptFiles.Select(Path.GetFileName))}");
        }

        // Add k6 load testing
        var k6ResourceName = $"k6-{Path.GetFileNameWithoutExtension(scriptName)}";
        Console.WriteLine($"[K6 AppHost] K6 resource name: {k6ResourceName}");
        Console.WriteLine($"[K6 AppHost] Script path in container: /scripts/{scriptName}");

        var k6 = builder.AddK6(k6ResourceName)
            .WithBindMount(scriptsPath, "/scripts", isReadOnly: true)
            .WithScript($"/scripts/{scriptName}")
            .WithReference(augmentService)
            .WaitFor(augmentService);

        return builder;
    }
}
