using AugmentService.Application.Services;
using AugmentService.Core.Entities;
using AugmentService.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AugmentService.Application.UnitTests.Services;

/// <summary>
/// Unit tests for AuthorizationService.
/// Tests permission aggregation logic and business rules.
/// </summary>
public class AuthorizationServiceTests
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationServiceTests()
    {
        _userRoleRepository = Substitute.For<IUserRoleRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<AuthorizationService>>();
    }

    private AuthorizationService CreateService()
    {
        return new AuthorizationService(_userRoleRepository, _roleRepository, _memoryCache, _logger);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithSingleRole_ReturnsCorrectPermissions()
    {
        // Arrange
        var service = CreateService();
        var userId = Guid.NewGuid();
        var readerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Reader",
            Description = "Read-only access",
            Permissions = new List<string> { "System.Read" },
            Rank = 1,
            IsActive = true
        };

        _userRoleRepository.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Role> { readerRole });

        _userRoleRepository.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "System.Read" });

        // Act
        var result = await service.GetUserPermissionsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Roles.Should().ContainSingle();
        result.Roles[0].Name.Should().Be("Reader");
        result.Permissions.Should().ContainSingle();
        result.Permissions.Should().Contain("System.Read");

        await _userRoleRepository.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithMultipleRoles_ReturnsDistinctUnionOfPermissions()
    {
        // Arrange
        var service = CreateService();
        var userId = Guid.NewGuid();
        var readerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Reader",
            Description = "Read-only access",
            Permissions = new List<string> { "System.Read" },
            Rank = 1,
            IsActive = true
        };

        var writerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Writer",
            Description = "Read and write access",
            Permissions = new List<string> { "System.Read", "System.Write" },
            Rank = 50,
            IsActive = true
        };

        _userRoleRepository.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Role> { readerRole, writerRole });

        _userRoleRepository.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "System.Read", "System.Write" });

        // Act
        var result = await service.GetUserPermissionsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Permissions.Should().HaveCount(2);
        result.Permissions.Should().Contain("System.Read");
        result.Permissions.Should().Contain("System.Write");
        result.Roles.Should().HaveCount(2);

        await _userRoleRepository.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithNoAssignedRoles_ReturnsEmptyPermissionsArray()
    {
        // Arrange
        var service = CreateService();
        var userId = Guid.NewGuid();

        _userRoleRepository.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Role>());

        _userRoleRepository.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        var result = await service.GetUserPermissionsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Permissions.Should().BeEmpty();
        result.Roles.Should().BeEmpty();

        await _userRoleRepository.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    // User Story 2 Tests: HasPermissionAsync with caching (T035-T037)

    [Fact]
    public async Task HasPermissionAsync_WhenUserHasPermission_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var userId = Guid.NewGuid();
        var permission = "System.Write";

        var writerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Writer",
            Description = "Read and write access",
            Permissions = new List<string> { "System.Read", "System.Write" },
            Rank = 50,
            IsActive = true
        };

        _userRoleRepository.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Role> { writerRole });

        _userRoleRepository.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "System.Read", "System.Write" });

        // Act
        var result = await service.HasPermissionAsync(userId, permission);

        // Assert
        result.Should().BeTrue();

        // Verify GetUserPermissionsAsync was called (which calls repository)
        await _userRoleRepository.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserLacksPermission_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var userId = Guid.NewGuid();
        var permission = "System.Admin";

        var readerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Reader",
            Description = "Read-only access",
            Permissions = new List<string> { "System.Read" },
            Rank = 1,
            IsActive = true
        };

        _userRoleRepository.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Role> { readerRole });

        _userRoleRepository.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "System.Read" });

        // Act
        var result = await service.HasPermissionAsync(userId, permission);

        // Assert
        result.Should().BeFalse();

        await _userRoleRepository.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasPermissionAsync_WithNonExistentPermission_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var userId = Guid.NewGuid();
        var permission = "InvalidPermission.DoesNotExist";

        _userRoleRepository.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Role>());

        _userRoleRepository.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        var result = await service.HasPermissionAsync(userId, permission);

        // Assert
        result.Should().BeFalse();
    }

    // User Story 3 Test: GetAllRolesAsync

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllActiveRoles()
    {
        // Arrange
        var service = CreateService();
        var roles = new List<Role>
        {
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Administrator",
                Description = "Full access",
                Permissions = new List<string> { "System.Read", "System.Write", "System.Admin" },
                Rank = 999,
                IsActive = true
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Reader",
                Description = "Read-only access",
                Permissions = new List<string> { "System.Read" },
                Rank = 1,
                IsActive = true
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Writer",
                Description = "Read and write access",
                Permissions = new List<string> { "System.Read", "System.Write" },
                Rank = 50,
                IsActive = true
            }
        };

        _roleRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(roles);

        // Act
        var result = await service.GetAllRolesAsync();

        // Assert
        result.Should().NotBeNull();
        var resultList = result.ToList();
        resultList.Should().HaveCount(3);
        resultList.Should().Contain(r => r.Name == "Administrator");
        resultList.Should().Contain(r => r.Name == "Reader");
        resultList.Should().Contain(r => r.Name == "Writer");

        await _roleRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }
}
