using AugmentService.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AugmentService.Api.UnitTests.Configuration;

public class RateLimitingOptionsTests
{
    [Fact]
    public void RateLimitingOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new RateLimitingOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(100, options.PermitLimit);
        Assert.Equal(60, options.WindowSeconds);
        Assert.Equal(2, options.QueueLimit);
    }

    [Fact]
    public void RateLimitingOptions_CanBindFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"] = "false",
                ["RateLimiting:PermitLimit"] = "200",
                ["RateLimiting:WindowSeconds"] = "120",
                ["RateLimiting:QueueLimit"] = "5"
            })
            .Build();

        // Act
        var options = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.False(options.Enabled);
        Assert.Equal(200, options.PermitLimit);
        Assert.Equal(120, options.WindowSeconds);
        Assert.Equal(5, options.QueueLimit);
    }

    [Fact]
    public void RateLimitingOptions_SectionName_IsCorrect()
    {
        // Arrange & Act
        var sectionName = RateLimitingOptions.SectionName;

        // Assert
        Assert.Equal("RateLimiting", sectionName);
    }

    [Fact]
    public void RateLimitingOptions_UsesDefaultsWhenConfigurationMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var options = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        // Assert - Should use default values
        Assert.True(options.Enabled);
        Assert.Equal(100, options.PermitLimit);
        Assert.Equal(60, options.WindowSeconds);
        Assert.Equal(2, options.QueueLimit);
    }

    [Fact]
    public void RateLimitingOptions_CanBindPartialConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "500"
                // Other properties will use defaults
            })
            .Build();

        // Act
        var options = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        // Assert
        Assert.Equal(500, options.PermitLimit); // Configured value
        Assert.True(options.Enabled); // Default value
        Assert.Equal(60, options.WindowSeconds); // Default value
        Assert.Equal(2, options.QueueLimit); // Default value
    }
}
