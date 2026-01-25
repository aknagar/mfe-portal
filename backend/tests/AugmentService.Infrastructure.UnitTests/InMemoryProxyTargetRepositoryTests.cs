using AugmentService.Core.Entities;
using AugmentService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AugmentService.Infrastructure.UnitTests;

public class InMemoryProxyTargetRepositoryTests
{
    private readonly ILogger<InMemoryProxyTargetRepository> _logger;
    private readonly InMemoryProxyTargetRepository _sut;

    public InMemoryProxyTargetRepositoryTests()
    {
        _logger = Substitute.For<ILogger<InMemoryProxyTargetRepository>>();
        _sut = new InMemoryProxyTargetRepository(_logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new InMemoryProxyTargetRepository(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task AddAsync_WithValidTarget_AddsAndReturnsTarget()
    {
        // Arrange
        var target = new ProxyTarget
        {
            Name = "TestProxy",
            BaseUrl = "https://api.example.com"
        };

        // Act
        var result = await _sut.AddAsync(target);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(target);
    }

    [Fact]
    public async Task AddAsync_WithNullTarget_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.AddAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTargetExists_ReturnsTarget()
    {
        // Arrange
        var target = new ProxyTarget
        {
            Name = "TestProxy",
            BaseUrl = "https://api.example.com"
        };
        await _sut.AddAsync(target);

        // Act
        var result = await _sut.GetByIdAsync(target.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(target);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTargetDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyCollection()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenTargetsExist_ReturnsAllTargets()
    {
        // Arrange
        var target1 = new ProxyTarget { Name = "Proxy1", BaseUrl = "https://api1.example.com" };
        var target2 = new ProxyTarget { Name = "Proxy2", BaseUrl = "https://api2.example.com" };
        await _sut.AddAsync(target1);
        await _sut.AddAsync(target2);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Name == "Proxy1");
        result.Should().Contain(t => t.Name == "Proxy2");
    }

    [Fact]
    public async Task DeleteAsync_WhenTargetExists_RemovesAndReturnsTrue()
    {
        // Arrange
        var target = new ProxyTarget
        {
            Name = "TestProxy",
            BaseUrl = "https://api.example.com"
        };
        await _sut.AddAsync(target);

        // Act
        var result = await _sut.DeleteAsync(target.Id);
        var deletedTarget = await _sut.GetByIdAsync(target.Id);

        // Assert
        result.Should().BeTrue();
        deletedTarget.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenTargetDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WhenTargetExists_UpdatesAndReturnsTarget()
    {
        // Arrange
        var target = new ProxyTarget
        {
            Name = "OriginalProxy",
            BaseUrl = "https://api.example.com"
        };
        await _sut.AddAsync(target);

        var updatedTarget = new ProxyTarget
        {
            Id = target.Id,
            Name = "UpdatedProxy",
            BaseUrl = "https://new-api.example.com",
            IsActive = false,
            TimeoutSeconds = 60
        };

        // Act
        var result = await _sut.UpdateAsync(updatedTarget);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("UpdatedProxy");
        result.BaseUrl.Should().Be("https://new-api.example.com");
        result.IsActive.Should().BeFalse();
        result.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public async Task UpdateAsync_WhenTargetDoesNotExist_ReturnsNull()
    {
        // Arrange
        var target = new ProxyTarget
        {
            Id = Guid.NewGuid(),
            Name = "NonExistentProxy",
            BaseUrl = "https://api.example.com"
        };

        // Act
        var result = await _sut.UpdateAsync(target);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithNullTarget_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.UpdateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddAsync_WithSameId_OverwritesExistingTarget()
    {
        // Arrange
        var id = Guid.NewGuid();
        var target1 = new ProxyTarget
        {
            Id = id,
            Name = "FirstProxy",
            BaseUrl = "https://first.example.com"
        };
        var target2 = new ProxyTarget
        {
            Id = id,
            Name = "SecondProxy",
            BaseUrl = "https://second.example.com"
        };

        // Act
        await _sut.AddAsync(target1);
        await _sut.AddAsync(target2);
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("SecondProxy");
        result.BaseUrl.Should().Be("https://second.example.com");
    }

    [Fact]
    public async Task Operations_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var target = new ProxyTarget
        {
            Name = "TestProxy",
            BaseUrl = "https://api.example.com"
        };
        var cts = new CancellationTokenSource();

        // Act & Assert - All operations should work with cancellation token
        var addResult = await _sut.AddAsync(target, cts.Token);
        addResult.Should().NotBeNull();

        var getResult = await _sut.GetByIdAsync(target.Id, cts.Token);
        getResult.Should().NotBeNull();

        var getAllResult = await _sut.GetAllAsync(cts.Token);
        getAllResult.Should().NotBeEmpty();

        var updateResult = await _sut.UpdateAsync(target, cts.Token);
        updateResult.Should().NotBeNull();

        var deleteResult = await _sut.DeleteAsync(target.Id, cts.Token);
        deleteResult.Should().BeTrue();
    }
}
