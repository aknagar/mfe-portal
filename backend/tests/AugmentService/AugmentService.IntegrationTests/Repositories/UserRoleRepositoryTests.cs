using AugmentService.Core.Entities;
using AugmentService.Infrastructure;
using AugmentService.Infrastructure.Data;
using AugmentService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace AugmentService.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for UserRoleRepository using TestContainers PostgreSQL.
/// Tests permission aggregation with JSONB and user-role assignments.
/// </summary>
public class UserRoleRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AuthorizationDbContext _context = null!;
    private UserRoleRepository _repository = null!;
    private UserRepository _userRepository = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var config = Options.Create(new InfrastructureConfig
        {
            ConnectionString = _postgres.GetConnectionString(),
            EnableSensitiveDataLogging = true
        });

        _context = new AuthorizationDbContext(options, config);
        await _context.Database.EnsureCreatedAsync();

        _repository = new UserRoleRepository(_context);
        _userRepository = new UserRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserPermissionsAsync_WithMultipleRoles_AggregatesPermissionsCorrectly()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        await _userRepository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var writerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = readerRoleId });
        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = writerRoleId });

        // Act
        var permissions = await _repository.GetUserPermissionsAsync(user.UserId);

        // Assert
        permissions.Should().NotBeNull();
        permissions.Should().HaveCount(2);
        permissions.Should().Contain("System.Read");
        permissions.Should().Contain("System.Write");
        // Distinct should eliminate duplicate "System.Read" from both Reader and Writer roles
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserRolesAsync_WithMultipleRoles_ReturnsRolesOrderedByRank()
    {
        // Arrange
        var user = new User { Email = "admin@example.com" };
        await _userRepository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Rank 1
        var writerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002"); // Rank 50
        var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003"); // Rank 999

        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = readerRoleId });
        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = adminRoleId });
        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = writerRoleId });

        // Act
        var roles = await _repository.GetUserRolesAsync(user.UserId);

        // Assert
        roles.Should().NotBeNull();
        roles.Should().HaveCount(3);
        roles.Should().BeInDescendingOrder(r => r.Rank);
        roles.First().Name.Should().Be("Administrator"); // Highest rank first
        roles.Last().Name.Should().Be("Reader"); // Lowest rank last
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HasPermissionAsync_WithUserHavingPermission_ReturnsTrue()
    {
        // Arrange
        var user = new User { Email = "writer@example.com" };
        await _userRepository.AddAsync(user);

        var writerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = writerRoleId });

        // Act
        var result = await _repository.HasPermissionAsync(user.UserId, "System.Write");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HasPermissionAsync_WithUserLackingPermission_ReturnsFalse()
    {
        // Arrange
        var user = new User { Email = "reader@example.com" };
        await _userRepository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = readerRoleId });

        // Act
        var result = await _repository.HasPermissionAsync(user.UserId, "System.Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPrimaryRoleAsync_WithMultipleRoles_ReturnsHighestRankRole()
    {
        // Arrange
        var user = new User { Email = "multi@example.com" };
        await _userRepository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Rank 1
        var writerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002"); // Rank 50

        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = readerRoleId });
        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = writerRoleId });

        // Act
        var primaryRole = await _repository.GetPrimaryRoleAsync(user.UserId);

        // Assert
        primaryRole.Should().NotBeNull();
        primaryRole!.Name.Should().Be("Writer");
        primaryRole.Rank.Should().Be(50);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddAsync_WithDuplicateAssignment_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User { Email = "duplicate@example.com" };
        await _userRepository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = readerRoleId });

        // Act & Assert
        var act = async () => await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = readerRoleId });
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already assigned*");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RemoveAsync_WithExistingAssignment_RemovesSuccessfully()
    {
        // Arrange
        var user = new User { Email = "remove@example.com" };
        await _userRepository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await _repository.AddAsync(new UserRole { UserId = user.UserId, RoleId = readerRoleId });

        // Act
        await _repository.RemoveAsync(user.UserId, readerRoleId);

        // Assert
        var roles = await _repository.GetUserRolesAsync(user.UserId);
        roles.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserPermissionsAsync_WithNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var user = new User { Email = "noroles@example.com" };
        await _userRepository.AddAsync(user);

        // Act
        var permissions = await _repository.GetUserPermissionsAsync(user.UserId);

        // Assert
        permissions.Should().NotBeNull();
        permissions.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HasPermissionAsync_WithNullOrEmptyPermission_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var actNull = async () => await _repository.HasPermissionAsync(userId, null!);
        await actNull.Should().ThrowAsync<ArgumentException>();

        var actEmpty = async () => await _repository.HasPermissionAsync(userId, string.Empty);
        await actEmpty.Should().ThrowAsync<ArgumentException>();
    }
}
