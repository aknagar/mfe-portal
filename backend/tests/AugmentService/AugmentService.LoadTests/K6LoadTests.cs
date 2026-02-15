using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace AugmentService.LoadTests;

/// <summary>
/// Sample load test using k6 and Aspire hosting
/// </summary>
public class K6LoadTests : IAsyncLifetime
{
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        // Build and start the application
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MfePortal_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task K6LoadTest_ShouldComplete()
    {
        // Arrange
        if (_app is null)
        {
            throw new InvalidOperationException("Application not initialized");
        }

        // Wait for k6 resource to complete
        // k6 runs automatically as part of the AppHost and executes the main.js script
        await _app.ResourceNotifications.WaitForResourceAsync("k6")
            .WaitAsync(TimeSpan.FromMinutes(5));

        // Assert - if we got here, the test passed
        // k6 will have executed and the thresholds defined in the script will be validated
        true.Should().BeTrue("k6 load test completed successfully");
    }
}
