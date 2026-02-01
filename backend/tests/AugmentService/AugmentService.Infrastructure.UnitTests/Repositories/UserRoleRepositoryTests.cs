using AugmentService.Core.Entities;
using AugmentService.Infrastructure;
using AugmentService.Infrastructure.Data;
using AugmentService.Infrastructure.Repositories;
using Common.Builders;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AugmentService.Infrastructure.UnitTests.Repositories;

public class UserRoleRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<UserDbContext> _contextOptions;

    public UserRoleRepositoryTests()
    {
        // Create and open a connection for the in-memory database
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        // Create context options using the in-memory SQLite connection
        _contextOptions = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the schema
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private UserDbContext CreateContext()
    {
        var config = Options.Create(new InfrastructureConfig
        {
            ConnectionString = "Data Source=:memory:",
            EnableSensitiveDataLogging = false
        });

        return new UserDbContext(_contextOptions, config);
    }

    private async Task ClearSeedData(UserDbContext context)
    {
        // Remove seed data to ensure test isolation
        var seedUserRoles = await context.UserRoles.ToListAsync();
        context.UserRoles.RemoveRange(seedUserRoles);
        
        var seedRoles = await context.Roles.Where(r => 
            r.Name == "Reader" || r.Name == "Writer" || r.Name == "Administrator").ToListAsync();
        context.Roles.RemoveRange(seedRoles);
        
        var seedUsers = await context.Users.Where(u => 
            u.Email == "akashnagar47@outlook.com").ToListAsync();
        context.Users.RemoveRange(seedUsers);
        
        await context.SaveChangesAsync();
    }

    private async Task<User> CreateUserWithRoles(UserDbContext context, params Role[] roles)
    {
        await ClearSeedData(context);
        
        var user = UserBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.Roles.AddRangeAsync(roles);

        foreach (var role in roles)
        {
            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = role.Id
            };
            await context.UserRoles.AddAsync(userRole);
        }

        await context.SaveChangesAsync();
        return user;
    }

    #region Constructor Tests

    [Fact]
    public void Should_ThrowArgumentNullException_When_ContextIsNull()
    {
        // Act
        var act = () => new UserRoleRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region GetUserRolesAsync Tests

    [Fact]
    public async Task GetUserRolesAsync_Should_ReturnRolesForUser_When_UserHasMultipleRoles()
    {
        // Arrange
        using var context = CreateContext();
        var adminRole = RoleBuilder.CreateAdmin().Build();
        var writerRole = RoleBuilder.CreateWriter().Build();
        var user = await CreateUserWithRoles(context, adminRole, writerRole);

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetUserRolesAsync(user.UserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Id == adminRole.Id);
        result.Should().Contain(r => r.Id == writerRole.Id);
    }

    [Fact]
    public async Task GetUserRolesAsync_Should_ReturnEmptyList_When_UserHasNoRoles()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetUserRolesAsync(user.UserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserRolesAsync_Should_OrderByRankDescending()
    {
        // Arrange
        using var context = CreateContext();
        var role1 = RoleBuilder.CreateDefault().WithName("LowRank").WithRank(10).Build();
        var role2 = RoleBuilder.CreateDefault().WithName("MediumRank").WithRank(50).Build();
        var role3 = RoleBuilder.CreateDefault().WithName("HighRank").WithRank(100).Build();
        var user = await CreateUserWithRoles(context, role1, role2, role3);

        var repository = new UserRoleRepository(context);

        // Act
        var result = (await repository.GetUserRolesAsync(user.UserId)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Rank.Should().Be(100);
        result[1].Rank.Should().Be(50);
        result[2].Rank.Should().Be(10);
        result.Should().BeInDescendingOrder(r => r.Rank);
    }

    [Fact]
    public async Task GetUserRolesAsync_Should_OnlyReturnActiveRoles()
    {
        // Arrange
        using var context = CreateContext();
        var activeRole = RoleBuilder.CreateDefault().WithName("Active").Build();
        var inactiveRole = RoleBuilder.CreateDefault().WithName("Inactive").Build();
        inactiveRole.IsActive = false;

        var user = await CreateUserWithRoles(context, activeRole, inactiveRole);

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetUserRolesAsync(user.UserId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(r => r.Name == "Active");
        result.Should().NotContain(r => r.Name == "Inactive");
    }

    [Fact]
    public async Task GetUserRolesAsync_Should_SupportCancellationToken()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault().Build();
        var user = await CreateUserWithRoles(context, role);

        var repository = new UserRoleRepository(context);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await repository.GetUserRolesAsync(user.UserId, cts.Token);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetUserPermissionsAsync Tests

    [Fact(Skip = "SQLite doesn't support SelectMany with Distinct (requires SQL APPLY). See integration tests for coverage.")]
    public async Task GetUserPermissionsAsync_Should_ReturnDistinctPermissions_When_UserHasMultipleRoles()
    {
        // Arrange
        using var context = CreateContext();
        var role1 = RoleBuilder.CreateDefault()
            .WithName("Role1")
            .AddPermissions("read:data", "write:data")
            .Build();
        var role2 = RoleBuilder.CreateDefault()
            .WithName("Role2")
            .AddPermissions("read:data", "delete:data") // read:data is duplicate
            .Build();
        var user = await CreateUserWithRoles(context, role1, role2);

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetUserPermissionsAsync(user.UserId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("read:data");
        result.Should().Contain("write:data");
        result.Should().Contain("delete:data");
    }

    [Fact(Skip = "SQLite doesn't support SelectMany with Distinct (requires SQL APPLY). See integration tests for coverage.")]
    public async Task GetUserPermissionsAsync_Should_ReturnEmptyList_When_UserHasNoRoles()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetUserPermissionsAsync(user.UserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Skip = "SQLite doesn't support SelectMany with Distinct (requires SQL APPLY). See integration tests for coverage.")]
    public async Task GetUserPermissionsAsync_Should_AggregatePermissionsFromAllRoles()
    {
        // Arrange
        using var context = CreateContext();
        var adminRole = RoleBuilder.CreateAdmin().Build();
        var writerRole = RoleBuilder.CreateWriter().Build();
        var user = await CreateUserWithRoles(context, adminRole, writerRole);

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetUserPermissionsAsync(user.UserId);

        // Assert
        result.Should().NotBeEmpty();
        // Admin has all permissions, Writer has subset, should get all unique permissions
        result.Should().Contain(adminRole.Permissions);
    }

    [Fact(Skip = "SQLite doesn't support SelectMany with Distinct (requires SQL APPLY). See integration tests for coverage.")]
    public async Task GetUserPermissionsAsync_Should_SupportCancellationToken()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .AddPermissions("test:permission")
            .Build();
        var user = await CreateUserWithRoles(context, role);

        var repository = new UserRoleRepository(context);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await repository.GetUserPermissionsAsync(user.UserId, cts.Token);

        // Assert
        result.Should().NotBeEmpty();
    }

    #endregion

    #region HasPermissionAsync Tests

    [Fact]
    public async Task HasPermissionAsync_Should_ReturnTrue_When_UserHasPermission()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .AddPermissions("read:data", "write:data")
            .Build();
        var user = await CreateUserWithRoles(context, role);

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.HasPermissionAsync(user.UserId, "read:data");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_Should_ReturnFalse_When_UserDoesNotHavePermission()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .AddPermissions("read:data")
            .Build();
        var user = await CreateUserWithRoles(context, role);

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.HasPermissionAsync(user.UserId, "delete:data");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_Should_ThrowArgumentException_When_PermissionIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRoleRepository(context);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await repository.HasPermissionAsync(userId, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("permission");
    }

    [Fact]
    public async Task HasPermissionAsync_Should_ThrowArgumentException_When_PermissionIsEmpty()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRoleRepository(context);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await repository.HasPermissionAsync(userId, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("permission");
    }

    [Fact]
    public async Task HasPermissionAsync_Should_ThrowArgumentException_When_PermissionIsWhitespace()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRoleRepository(context);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await repository.HasPermissionAsync(userId, "   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("permission");
    }

    [Fact]
    public async Task HasPermissionAsync_Should_CheckAcrossAllUserRoles()
    {
        // Arrange
        using var context = CreateContext();
        var role1 = RoleBuilder.CreateDefault()
            .WithName("Role1")
            .AddPermissions("permission1")
            .Build();
        var role2 = RoleBuilder.CreateDefault()
            .WithName("Role2")
            .AddPermissions("permission2")
            .Build();
        var user = await CreateUserWithRoles(context, role1, role2);

        var repository = new UserRoleRepository(context);

        // Act
        var hasPermission1 = await repository.HasPermissionAsync(user.UserId, "permission1");
        var hasPermission2 = await repository.HasPermissionAsync(user.UserId, "permission2");

        // Assert
        hasPermission1.Should().BeTrue();
        hasPermission2.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_Should_OnlyCheckActiveRoles()
    {
        // Arrange
        using var context = CreateContext();
        var activeRole = RoleBuilder.CreateDefault()
            .WithName("Active")
            .AddPermissions("active:permission")
            .Build();
        var inactiveRole = RoleBuilder.CreateDefault()
            .WithName("Inactive")
            .AddPermissions("inactive:permission")
            .Build();
        inactiveRole.IsActive = false;

        var user = await CreateUserWithRoles(context, activeRole, inactiveRole);

        var repository = new UserRoleRepository(context);

        // Act
        var hasActivePermission = await repository.HasPermissionAsync(user.UserId, "active:permission");
        var hasInactivePermission = await repository.HasPermissionAsync(user.UserId, "inactive:permission");

        // Assert
        hasActivePermission.Should().BeTrue();
        hasInactivePermission.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_Should_SupportCancellationToken()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .AddPermissions("test:permission")
            .Build();
        var user = await CreateUserWithRoles(context, role);

        var repository = new UserRoleRepository(context);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await repository.HasPermissionAsync(user.UserId, "test:permission", cts.Token);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetPrimaryRoleAsync Tests

    [Fact]
    public async Task GetPrimaryRoleAsync_Should_ReturnHighestRankedRole()
    {
        // Arrange
        using var context = CreateContext();
        var lowRole = RoleBuilder.CreateDefault().WithName("Low").WithRank(10).Build();
        var midRole = RoleBuilder.CreateDefault().WithName("Mid").WithRank(50).Build();
        var highRole = RoleBuilder.CreateDefault().WithName("High").WithRank(100).Build();
        var user = await CreateUserWithRoles(context, lowRole, midRole, highRole);

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetPrimaryRoleAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("High");
        result.Rank.Should().Be(100);
    }

    [Fact]
    public async Task GetPrimaryRoleAsync_Should_ReturnNull_When_UserHasNoRoles()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetPrimaryRoleAsync(user.UserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPrimaryRoleAsync_Should_OnlyConsiderActiveRoles()
    {
        // Arrange
        using var context = CreateContext();
        var activeRole = RoleBuilder.CreateDefault().WithName("Active").WithRank(50).Build();
        var inactiveRole = RoleBuilder.CreateDefault().WithName("Inactive").WithRank(100).Build();
        inactiveRole.IsActive = false;

        var user = await CreateUserWithRoles(context, activeRole, inactiveRole);

        var repository = new UserRoleRepository(context);

        // Act
        var result = await repository.GetPrimaryRoleAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Active");
        result.Rank.Should().Be(50);
    }

    [Fact]
    public async Task GetPrimaryRoleAsync_Should_SupportCancellationToken()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault().Build();
        var user = await CreateUserWithRoles(context, role);

        var repository = new UserRoleRepository(context);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await repository.GetPrimaryRoleAsync(user.UserId, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_Should_AddUserRoleAssignment()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        var role = RoleBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };

        // Act
        var result = await repository.AddAsync(userRole);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.UserId);
        result.RoleId.Should().Be(role.Id);

        // Verify it was saved to database
        var savedUserRole = await context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.Id);
        savedUserRole.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_Should_ThrowArgumentNullException_When_UserRoleIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRoleRepository(context);

        // Act
        var act = async () => await repository.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("userRole");
    }

    [Fact]
    public async Task AddAsync_Should_ThrowInvalidOperationException_When_AssignmentAlreadyExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        var role = RoleBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);
        var userRole1 = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };
        await repository.AddAsync(userRole1);

        var userRole2 = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };

        // Act
        var act = async () => await repository.AddAsync(userRole2);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already assigned*");
    }

    [Fact]
    public async Task AddAsync_Should_SaveToDatabase()
    {
        // Arrange
        using var context = CreateContext();
        await ClearSeedData(context);
        
        var user = UserBuilder.CreateDefault().Build();
        var role = RoleBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };

        // Act
        await repository.AddAsync(userRole);

        // Assert
        var count = await context.UserRoles.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_Should_SupportCancellationToken()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        var role = RoleBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };
        using var cts = new CancellationTokenSource();

        // Act
        var result = await repository.AddAsync(userRole, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_Should_RemoveUserRoleAssignment()
    {
        // Arrange
        using var context = CreateContext();
        await ClearSeedData(context);
        
        var user = UserBuilder.CreateDefault().Build();
        var role = RoleBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.Roles.AddAsync(role);
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };
        await context.UserRoles.AddAsync(userRole);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);

        // Act
        await repository.RemoveAsync(user.UserId, role.Id);

        // Assert
        var deletedUserRole = await context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.Id);
        deletedUserRole.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_Should_NotThrow_When_AssignmentDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRoleRepository(context);
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        // Act
        var act = async () => await repository.RemoveAsync(userId, roleId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_Should_DeleteFromDatabase()
    {
        // Arrange
        using var context = CreateContext();
        await ClearSeedData(context);
        
        var user = UserBuilder.CreateDefault().Build();
        var role = RoleBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.Roles.AddAsync(role);
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };
        await context.UserRoles.AddAsync(userRole);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);

        // Act
        await repository.RemoveAsync(user.UserId, role.Id);

        // Assert
        var count = await context.UserRoles.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task RemoveAsync_Should_SupportCancellationToken()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        var role = RoleBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.Roles.AddAsync(role);
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };
        await context.UserRoles.AddAsync(userRole);
        await context.SaveChangesAsync();

        var repository = new UserRoleRepository(context);
        using var cts = new CancellationTokenSource();

        // Act
        await repository.RemoveAsync(user.UserId, role.Id, cts.Token);

        // Assert
        var deletedUserRole = await context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.Id);
        deletedUserRole.Should().BeNull();
    }

    #endregion
}
