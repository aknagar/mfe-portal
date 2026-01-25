using System.Net;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AugmentService.E2eTests;

public class AppHostTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    [Trait("Category", "E2E")]
    public async Task AppHost_StartsSuccessfully()
    {
        // Arrange
        var cts = new CancellationTokenSource(DefaultTimeout);
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MfePortal_AppHost>(cts.Token);

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Warning);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            clientBuilder.AddStandardResilienceHandler());

        await using var app = await appHost.BuildAsync(cts.Token)
            .WaitAsync(DefaultTimeout, cts.Token);

        await app.StartAsync(cts.Token)
            .WaitAsync(DefaultTimeout, cts.Token);

        // Act & Assert - App started successfully
        app.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task AugmentServiceApi_RespondsToHealthCheck()
    {
        // Arrange
        var cts = new CancellationTokenSource(DefaultTimeout);
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MfePortal_AppHost>(cts.Token);

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Error);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            clientBuilder.AddStandardResilienceHandler());

        await using var app = await appHost.BuildAsync(cts.Token)
            .WaitAsync(DefaultTimeout, cts.Token);

        await app.StartAsync(cts.Token)
            .WaitAsync(DefaultTimeout, cts.Token);

        // Act
        using var httpClient = app.CreateHttpClient("augmentservice-api");
        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            "augmentservice-api", cts.Token)
            .WaitAsync(DefaultTimeout, cts.Token);

        using var response = await httpClient.GetAsync("/health", cts.Token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
