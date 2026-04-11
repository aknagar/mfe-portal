using System.Net;
using FluentAssertions;
using Xunit;

namespace AugmentService.IntegrationTests.Api;

public class RootEndpointTests : IClassFixture<WeatherTestFactory>
{
    private readonly WeatherTestFactory _factory;

    public RootEndpointTests(WeatherTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Get_Root_ReturnsOkWithOkBody()
    {
        // Arrange — unauthenticated client (endpoint must be anonymous)
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("ok");
    }
}
