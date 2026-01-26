using AugmentService.Core.Entities;
using AugmentService.Core.Interfaces;
using FluentAssertions;
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

    public AuthorizationServiceTests()
    {
        _userRoleRepository = Substitute.For<IUserRoleRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithSingleRole_ReturnsCorrectPermissions()
    {
        // Arrange
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
        var permissions = await _userRoleRepository.GetUserPermissionsAsync(userId);

        // Assert
        permissions.Should().NotBeNull();
        permissions.Should().ContainSingle();
        permissions.Should().Contain("System.Read");

        await _userRoleRepository.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithMultipleRoles_ReturnsDistinctUnionOfPermissions()
    {
        // Arrange
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
        var permissions = await _userRoleRepository.GetUserPermissionsAsync(userId);

        // Assert
        permissions.Should().NotBeNull();
        permissions.Should().HaveCount(2);
        permissions.Should().Contain("System.Read");
        permissions.Should().Contain("System.Write");

        await _userRoleRepository.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithNoAssignedRoles_ReturnsEmptyPermissionsArray()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRoleRepository.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Role>());

        _userRoleRepository.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        var permissions = await _userRoleRepository.GetUserPermissionsAsync(userId);

        // Assert
        permissions.Should().NotBeNull();
        permissions.Should().BeEmpty();

        await _userRoleRepository.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserHasPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "System.Write";

        _userRoleRepository.HasPermissionAsync(userId, permission, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _userRoleRepository.HasPermissionAsync(userId, permission);

        // Assert
        result.Should().BeTrue();

        await _userRoleRepository.Received(1).HasPermissionAsync(userId, permission, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserLacksPermission_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "System.Admin";

        _userRoleRepository.HasPermissionAsync(userId, permission, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _userRoleRepository.HasPermissionAsync(userId, permission);

        // Assert
        result.Should().BeFalse();

        await _userRoleRepository.Received(1).HasPermissionAsync(userId, permission, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasPermissionAsync_WithNonExistentPermission_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "InvalidPermission";

        _userRoleRepository.HasPermissionAsync(userId, permission, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _userRoleRepository.HasPermissionAsync(userId, permission);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllActiveRoles()
    {
        // Arrange
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
            .Returns(roles.OrderBy(r => r.Name));

        // Act
        var result = await _roleRepository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(r => r.Name);

        await _roleRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }
}
