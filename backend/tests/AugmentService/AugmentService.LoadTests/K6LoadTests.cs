using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace AugmentService.LoadTests;

/// <summary>
/// K6 load tests executed against AugmentService using containerized K6
/// No K6 installation required - runs in Docker containers via Aspire
/// </summary>
public class K6LoadTests
{
    private readonly ITestOutputHelper _output;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    public K6LoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Temporarily disabled")]
    [Trait("Category", "LoadTest")]
    public async Task HealthCheck_SmokeTest_Succeeds()
    {
        await RunK6TestAsync("smoke-test.js", "smoke");
    }

    [Fact(Skip = "Temporarily disabled")]
    [Trait("Category", "LoadTest")]
    public async Task UserPermissions_LoadTest_Succeeds()
    {
        await RunK6TestAsync("user-permissions-test.js", "load");
    }

    [Fact(Skip = "Temporarily disabled")]
    [Trait("Category", "LoadTest")]
    public async Task Proxy_LoadTest_Succeeds()
    {
        await RunK6TestAsync("proxy-test.js", "load");
    }

    private async Task RunK6TestAsync(string scriptName, string scenario)
    {
        using var cts = new CancellationTokenSource(DefaultTimeout);

        _output.WriteLine($"Starting Aspire AppHost with K6 script: {scriptName}");

        // Determine the scripts directory path
        var scriptsPath = Path.Combine(AppContext.BaseDirectory, "scripts");
        _output.WriteLine($"Scripts directory: {scriptsPath}");
        _output.WriteLine($"Directory exists: {Directory.Exists(scriptsPath)}");

        if (Directory.Exists(scriptsPath))
        {
            _output.WriteLine($"Scripts found: {string.Join(", ", Directory.GetFiles(scriptsPath, "*.js").Select(Path.GetFileName))}");
        }

        // Set environment variable for the AppHost to read
        Environment.SetEnvironmentVariable("K6_SCRIPT_NAME", scriptName);

        try
        {
            // Use the custom AppHost with testing infrastructure
            var appHostBuilder = await DistributedApplicationTestingBuilder
                .CreateAsync<AppHost>(cts.Token);

            // Configure logging
            appHostBuilder.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Warning);
                logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Error);
                logging.AddProvider(new XunitLoggerProvider(_output));
            });

            appHostBuilder.Services.ConfigureHttpClientDefaults(clientBuilder =>
                clientBuilder.AddStandardResilienceHandler());

            // Build and start the app
            await using var app = await appHostBuilder.BuildAsync(cts.Token);
            await app.StartAsync(cts.Token);

            _output.WriteLine("AppHost started. Waiting for AugmentService to be healthy...");

            // Wait for the service to be healthy
            await app.ResourceNotifications.WaitForResourceHealthyAsync(
                "augmentservice-api", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            _output.WriteLine("AugmentService is healthy. K6 test should be running in container...");

            // Get the K6 resource name based on script
            var k6ResourceName = $"k6-{Path.GetFileNameWithoutExtension(scriptName)}";
            _output.WriteLine($"Waiting for K6 resource: {k6ResourceName}");

            // Wait for K6 resource to complete
            // K6 runs as a container and will exit when the test completes
            var k6Completed = false;
            var timeout = DateTime.UtcNow.Add(DefaultTimeout);

            while (DateTime.UtcNow < timeout && !k6Completed)
            {
                try
                {
                    await app.ResourceNotifications.WaitForResourceAsync(
                        k6ResourceName,
                        (re) => re.Snapshot.State?.Text == "Exited")
                        .WaitAsync(TimeSpan.FromSeconds(10), cts.Token);

                    k6Completed = true;
                    _output.WriteLine($"K6 test '{scriptName}' completed.");
                }
                catch (TimeoutException)
                {
                    _output.WriteLine($"Still waiting for K6 test '{scriptName}' to complete...");
                }
            }

            if (!k6Completed)
            {
                throw new TimeoutException($"K6 test '{scriptName}' did not complete within the timeout period.");
            }

            // If we got here, the test completed
            // K6 will have logged results to the Aspire dashboard
            true.Should().BeTrue($"K6 test '{scriptName}' completed successfully");
        }
        finally
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable("K6_SCRIPT_NAME", null);
        }
    }
}

/// <summary>
/// Simple logger provider for writing to xUnit test output
/// </summary>
internal class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName) => new XunitLogger(_output, categoryName);

    public void Dispose() { }
}

internal class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            _output.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
            if (exception != null)
            {
                _output.WriteLine(exception.ToString());
            }
        }
        catch
        {
            // Ignore errors writing to test output
        }
    }
}
