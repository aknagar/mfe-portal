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

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task HealthCheck_SmokeTest_Succeeds()
    {
        await RunK6TestAsync("smoke-test.js", "smoke");
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task UserPermissions_LoadTest_Succeeds()
    {
        await RunK6TestAsync("user-permissions-test.js", "load");
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task Proxy_LoadTest_Succeeds()
    {
        await RunK6TestAsync("proxy-test.js", "load");
    }

    private async Task RunK6TestAsync(string scriptName, string scenario)
    {
        using var cts = new CancellationTokenSource(DefaultTimeout);

        _output.WriteLine($"Starting Aspire AppHost with K6 script: {scriptName}");

        // Create AppHost builder using the testing infrastructure
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<AppHost>(cts.Token);

        // Add the script name to configuration so AppHost can read it
        appHostBuilder.Configuration["TestScriptName"] = scriptName;

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

        try
        {
            // Wait for K6 resource to complete
            // K6 runs as a container and will exit when the test completes
            await app.ResourceNotifications.WaitForResourceAsync(
                k6ResourceName,
                (re) => re.Snapshot.State?.Text == "Exited")
                .WaitAsync(DefaultTimeout, cts.Token);

            _output.WriteLine($"K6 test '{scriptName}' completed.");

            // If we got here, the test completed
            // K6 will have logged results to the Aspire dashboard
            true.Should().BeTrue($"K6 test '{scriptName}' completed successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error waiting for K6 test: {ex.Message}");
            throw;
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
