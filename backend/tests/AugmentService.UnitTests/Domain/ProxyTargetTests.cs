using AugmentService.Core.Entities;
using FluentAssertions;
using Xunit;

namespace AugmentService.UnitTests.Domain;

public class ProxyTargetTests
{
    [Fact]
    public void ProxyTarget_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var target = new ProxyTarget
        {
            Name = "TestProxy",
            BaseUrl = "https://api.example.com"
        };

        // Assert
        target.Id.Should().NotBe(Guid.Empty);
        target.Name.Should().Be("TestProxy");
        target.BaseUrl.Should().Be("https://api.example.com");
        target.IsActive.Should().BeTrue();
        target.TimeoutSeconds.Should().Be(30);
        target.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        target.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void ProxyTarget_CanBeDisabled()
    {
        // Arrange
        var target = new ProxyTarget
        {
            Name = "TestProxy",
            BaseUrl = "https://api.example.com"
        };

        // Act
        target.IsActive = false;

        // Assert
        target.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ProxyTarget_CustomTimeout_CanBeSet()
    {
        // Arrange & Act
        var target = new ProxyTarget
        {
            Name = "TestProxy",
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 60
        };

        // Assert
        target.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void ProxyTarget_UpdatedAt_CanBeSet()
    {
        // Arrange
        var target = new ProxyTarget
        {
            Name = "TestProxy",
            BaseUrl = "https://api.example.com"
        };
        var updateTime = DateTime.UtcNow;

        // Act
        target.UpdatedAt = updateTime;

        // Assert
        target.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void ProxyTarget_Id_CanBeOverridden()
    {
        // Arrange
        var customId = Guid.NewGuid();

        // Act
        var target = new ProxyTarget
        {
            Id = customId,
            Name = "TestProxy",
            BaseUrl = "https://api.example.com"
        };

        // Assert
        target.Id.Should().Be(customId);
    }
}
