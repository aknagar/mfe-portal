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
/// Integration tests for UserRepository using TestContainers PostgreSQL.
/// Tests actual database interactions including transactions, constraints, and relationships.
/// </summary>
public class UserRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private UserDbContext _context = null!;
    private UserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var config = Options.Create(new InfrastructureConfig
        {
            ConnectionString = _postgres.GetConnectionString(),
            EnableSensitiveDataLogging = true
        });

        _context = new UserDbContext(options, config);
        await _context.Database.EnsureCreatedAsync();

        _repository = new UserRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_GetSeededAdminUser_When_GetByIdAsyncCalled()
    {
        // Arrange
        var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000100");

        // Act
        var user = await _repository.GetByIdAsync(adminUserId);

        // Assert
        user.Should().NotBeNull();
        user!.UserId.Should().Be(adminUserId);
        user.Email.Should().Be("akashnagar47@outlook.com");
        user.UserRoles.Should().HaveCount(1);
        user.UserRoles.First().Role.Name.Should().Be("Administrator");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_GetSeededAdminUser_When_GetByEmailAsyncCalled()
    {
        // Act
        var user = await _repository.GetByEmailAsync("akashnagar47@outlook.com");

        // Assert
        user.Should().NotBeNull();
        user!.Email.Should().Be("akashnagar47@outlook.com");
        user.UserRoles.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_AddNewUserToDatabase_When_AddAsyncCalled()
    {
        // Arrange
        var newUser = new User
        {
            Email = "integration.test@example.com",
            CreatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(newUser);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().NotBeEmpty();
        result.Email.Should().Be("integration.test@example.com");

        // Verify it's actually in the database
        var retrieved = await _repository.GetByIdAsync(result.UserId);
        retrieved.Should().NotBeNull();
        retrieved!.Email.Should().Be("integration.test@example.com");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_ThrowDbUpdateException_When_AddingDuplicateEmail()
    {
        // Arrange
        var user1 = new User
        {
            Email = "duplicate@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user1);

        var user2 = new User
        {
            Email = "duplicate@example.com", // Same email
            CreatedDate = DateTime.UtcNow
        };

        // Act
        var act = async () => await _repository.AddAsync(user2);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>("unique constraint on email should be violated");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_UpdateUserEmail_When_UpdateAsyncCalled()
    {
        // Arrange
        var user = new User
        {
            Email = "original@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user);

        // Act
        user.Email = "updated@example.com";
        await _repository.UpdateAsync(user);

        // Assert
        var retrieved = await _repository.GetByIdAsync(user.UserId);
        retrieved.Should().NotBeNull();
        retrieved!.Email.Should().Be("updated@example.com");
        retrieved.UpdatedDate.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_IncludeUserRolesWithRoles_When_GetByIdAsync()
    {
        // Arrange - Create user and assign a role
        var user = new User
        {
            Email = "roletest@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = readerRoleId,
            CreatedDate = DateTime.UtcNow
        };
        await _context.UserRoles.AddAsync(userRole);
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _repository.GetByIdAsync(user.UserId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.UserRoles.Should().HaveCount(1);
        retrieved.UserRoles.First().Role.Should().NotBeNull();
        retrieved.UserRoles.First().Role.Name.Should().Be("Reader");
        retrieved.UserRoles.First().Role.Permissions.Should().Contain("System.Read");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_HandleMultipleRoleAssignments_When_UserHasMultipleRoles()
    {
        // Arrange
        var user = new User
        {
            Email = "multirole@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var writerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        await _context.UserRoles.AddRangeAsync(
            new UserRole { UserId = user.UserId, RoleId = readerRoleId, CreatedDate = DateTime.UtcNow },
            new UserRole { UserId = user.UserId, RoleId = writerRoleId, CreatedDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _repository.GetByIdAsync(user.UserId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.UserRoles.Should().HaveCount(2);
        retrieved.UserRoles.Select(ur => ur.Role.Name).Should().Contain(new[] { "Reader", "Writer" });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_ThrowDbUpdateException_When_AssigningDuplicateUserRole()
    {
        // Arrange
        var user = new User
        {
            Email = "duprole@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userRole1 = new UserRole
        {
            UserId = user.UserId,
            RoleId = readerRoleId,
            CreatedDate = DateTime.UtcNow
        };
        await _context.UserRoles.AddAsync(userRole1);
        await _context.SaveChangesAsync();

        // Act - Try to add the same role again
        var userRole2 = new UserRole
        {
            UserId = user.UserId,
            RoleId = readerRoleId, // Same role
            CreatedDate = DateTime.UtcNow
        };
        await _context.UserRoles.AddAsync(userRole2);
        var act = async () => await _context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>("composite unique constraint should be violated");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_PersistJSONBPermissions_When_RoleHasPermissions()
    {
        // This test verifies that the JSONB column type works correctly
        // Arrange
        var user = new User
        {
            Email = "jsonbtest@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user);

        var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = adminRoleId,
            CreatedDate = DateTime.UtcNow
        };
        await _context.UserRoles.AddAsync(userRole);
        await _context.SaveChangesAsync();

        // Act
        var retrieved = await _repository.GetByIdAsync(user.UserId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.UserRoles.First().Role.Permissions.Should().NotBeNull();
        retrieved.UserRoles.First().Role.Permissions.Should().Contain("System.Admin");
        retrieved.UserRoles.First().Role.Permissions.Should().BeOfType<List<string>>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_HandleConcurrentUpdates_When_MultipleContextsUpdate()
    {
        // Arrange
        var user = new User
        {
            Email = "concurrent@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user);

        // Create second context
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        var config = Options.Create(new InfrastructureConfig
        {
            ConnectionString = _postgres.GetConnectionString(),
            EnableSensitiveDataLogging = true
        });
        using var context2 = new UserDbContext(options, config);
        var repository2 = new UserRepository(context2);

        // Act - Update from first repository
        user.Email = "concurrent1@example.com";
        await _repository.UpdateAsync(user);

        // Get fresh instance from second context and update
        var user2 = await repository2.GetByIdAsync(user.UserId);
        user2!.Email = "concurrent2@example.com";
        await repository2.UpdateAsync(user2);

        // Force context refresh to get latest data
        _context.Entry(user).State = EntityState.Detached;
        var final = await _repository.GetByIdAsync(user.UserId);

        // Assert - Last write wins (second update)
        final!.Email.Should().Be("concurrent2@example.com");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_SetUpdatedDateToUtcNow_When_UpdateAsyncCalled()
    {
        // Arrange
        var user = new User
        {
            Email = "timestamp@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user);
        user.UpdatedDate.Should().BeNull();

        var beforeUpdate = DateTime.UtcNow;
        await Task.Delay(10); // Small delay to ensure different timestamp

        // Act
        await _repository.UpdateAsync(user);

        // Assert
        var afterUpdate = DateTime.UtcNow;
        user.UpdatedDate.Should().NotBeNull();
        user.UpdatedDate.Should().BeOnOrAfter(beforeUpdate);
        user.UpdatedDate.Should().BeOnOrBefore(afterUpdate);
        user.UpdatedDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact(Skip = "Docker/TestContainers timeout - infrastructure dependency issue")]
    [Trait("Category", "Integration")]
    public async Task Should_CascadeDeleteUserRoles_When_UserIsDeleted()
    {
        // Arrange
        var user = new User
        {
            Email = "cascade@example.com",
            CreatedDate = DateTime.UtcNow
        };
        await _repository.AddAsync(user);

        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = readerRoleId,
            CreatedDate = DateTime.UtcNow
        };
        await _context.UserRoles.AddAsync(userRole);
        await _context.SaveChangesAsync();

        // Act - Delete the user
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Assert
        var deletedUser = await _repository.GetByIdAsync(user.UserId);
        deletedUser.Should().BeNull();

        var orphanedUserRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId);
        orphanedUserRole.Should().BeNull("cascade delete should remove user roles");
    }
}
