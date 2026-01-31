using AugmentService.Core.Entities;
using AugmentService.Infrastructure.Data;
using AugmentService.Infrastructure.Repositories;
using Common.Builders;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AugmentService.Infrastructure.UnitTests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<UserDbContext> _contextOptions;

    public UserRepositoryTests()
    {
        // Create and open a connection for the in-memory database
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        // Create context options using the in-memory SQLite connection
        _contextOptions = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the schema and seed data
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

    [Fact]
    public void Should_ThrowArgumentNullException_When_ContextIsNull()
    {
        // Act
        var act = () => new UserRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task Should_ReturnUser_When_GetByIdAsyncWithExistingUserId()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByIdAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Should_ReturnNull_When_GetByIdAsyncWithNonExistentUserId()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_IncludeUserRolesAndRoles_When_GetByIdAsync()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .WithName($"TestRole_{Guid.NewGuid():N}")
            .Build();
        var user = UserBuilder.CreateDefault().Build();
        
        await context.Roles.AddAsync(role);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };
        await context.UserRoles.AddAsync(userRole);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByIdAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.UserRoles.Should().HaveCount(1);
        result.UserRoles.First().Role.Should().NotBeNull();
        result.UserRoles.First().Role.Name.Should().StartWith("TestRole_");
    }

    [Fact]
    public async Task Should_ReturnUser_When_GetByEmailAsyncWithExistingEmail()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault()
            .WithEmail("test@example.com")
            .Build();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.UserId.Should().Be(user.UserId);
    }

    [Fact]
    public async Task Should_ReturnNull_When_GetByEmailAsyncWithNonExistentEmail()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_GetByEmailAsyncWithNullEmail()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);

        // Act
        var act = async () => await repository.GetByEmailAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Email cannot be null or empty.*")
            .WithParameterName("email");
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_GetByEmailAsyncWithEmptyEmail()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);

        // Act
        var act = async () => await repository.GetByEmailAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Email cannot be null or empty.*")
            .WithParameterName("email");
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_GetByEmailAsyncWithWhitespaceEmail()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);

        // Act
        var act = async () => await repository.GetByEmailAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Email cannot be null or empty.*")
            .WithParameterName("email");
    }

    [Fact]
    public async Task Should_IncludeUserRolesAndRoles_When_GetByEmailAsync()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .WithName($"TestRole_{Guid.NewGuid():N}")
            .Build();
        var user = UserBuilder.CreateDefault()
            .WithEmail("reader@example.com")
            .Build();
        
        await context.Roles.AddAsync(role);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.Id
        };
        await context.UserRoles.AddAsync(userRole);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByEmailAsync("reader@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.UserRoles.Should().HaveCount(1);
        result.UserRoles.First().Role.Should().NotBeNull();
        result.UserRoles.First().Role.Name.Should().StartWith("TestRole_");
    }

    [Fact]
    public async Task Should_AddUserAndSave_When_AddAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = UserBuilder.CreateDefault()
            .WithEmail("newuser@example.com")
            .Build();

        // Act
        var result = await repository.AddAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.UserId);
        result.Email.Should().Be("newuser@example.com");

        // Verify it was saved to the database
        var savedUser = await context.Users.FindAsync(user.UserId);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Should_ThrowArgumentNullException_When_AddAsyncWithNullUser()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);

        // Act
        var act = async () => await repository.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("user");
    }

    [Fact]
    public async Task Should_ReturnAddedUser_When_AddAsyncSucceeds()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = UserBuilder.CreateDefault().Build();

        // Act
        var result = await repository.AddAsync(user);

        // Assert
        result.Should().BeSameAs(user);
    }

    [Fact]
    public async Task Should_UpdateUserAndSave_When_UpdateAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault()
            .WithEmail("original@example.com")
            .Build();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);
        user.Email = "updated@example.com";

        // Act
        await repository.UpdateAsync(user);

        // Assert
        var updatedUser = await context.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task Should_SetUpdatedDate_When_UpdateAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        user.UpdatedDate.Should().BeNull(); // Verify initial state

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await repository.UpdateAsync(user);

        // Assert
        var afterUpdate = DateTime.UtcNow;
        user.UpdatedDate.Should().NotBeNull();
        user.UpdatedDate.Should().BeOnOrAfter(beforeUpdate);
        user.UpdatedDate.Should().BeOnOrBefore(afterUpdate);
        user.UpdatedDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Should_ThrowArgumentNullException_When_UpdateAsyncWithNullUser()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);

        // Act
        var act = async () => await repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("user");
    }

    [Fact]
    public async Task Should_RespectCancellationToken_When_GetByIdAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.GetByIdAsync(Guid.NewGuid(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_RespectCancellationToken_When_GetByEmailAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.GetByEmailAsync("test@example.com", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_RespectCancellationToken_When_AddAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = UserBuilder.CreateDefault().Build();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.AddAsync(user, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_RespectCancellationToken_When_UpdateAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        var user = UserBuilder.CreateDefault().Build();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.UpdateAsync(user, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_HandleMultipleRoles_When_UserHasMultipleRoleAssignments()
    {
        // Arrange
        using var context = CreateContext();
        var role1 = RoleBuilder.CreateDefault()
            .WithName($"TestRole1_{Guid.NewGuid():N}")
            .Build();
        var role2 = RoleBuilder.CreateDefault()
            .WithName($"TestRole2_{Guid.NewGuid():N}")
            .Build();
        var user = UserBuilder.CreateDefault().Build();

        await context.Roles.AddRangeAsync(role1, role2);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var userRole1 = new UserRole { UserId = user.UserId, RoleId = role1.Id };
        var userRole2 = new UserRole { UserId = user.UserId, RoleId = role2.Id };
        await context.UserRoles.AddRangeAsync(userRole1, userRole2);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByIdAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.UserRoles.Should().HaveCount(2);
        result.UserRoles.Select(ur => ur.Role.Name).Should().OnlyContain(name => name.StartsWith("TestRole"));
    }
}
