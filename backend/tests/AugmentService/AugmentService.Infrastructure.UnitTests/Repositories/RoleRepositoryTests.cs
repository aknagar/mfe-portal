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

public class RoleRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<UserDbContext> _contextOptions;

    public RoleRepositoryTests()
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

    [Fact]
    public void Should_ThrowArgumentNullException_When_ContextIsNull()
    {
        // Act
        var act = () => new RoleRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnRole_When_RoleExists()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .WithName("TestRole")
            .Build();
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetByIdAsync(role.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(role.Id);
        result.Name.Should().Be("TestRole");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_RoleDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_RoleIsInactive()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .WithName("InactiveRole")
            .Build();
        role.IsActive = false;
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetByIdAsync(role.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_Should_ReturnRole_When_RoleExists()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .WithName("Administrator")
            .Build();
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetByNameAsync("Administrator");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Administrator");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByNameAsync_Should_ReturnNull_When_RoleDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_Should_ReturnNull_When_RoleIsInactive()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault()
            .WithName("InactiveRole")
            .Build();
        role.IsActive = false;
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetByNameAsync("InactiveRole");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_Should_ThrowArgumentException_When_NameIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);

        // Act
        var act = async () => await repository.GetByNameAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public async Task GetByNameAsync_Should_ThrowArgumentException_When_NameIsEmpty()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);

        // Act
        var act = async () => await repository.GetByNameAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public async Task GetByNameAsync_Should_ThrowArgumentException_When_NameIsWhitespace()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);

        // Act
        var act = async () => await repository.GetByNameAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public async Task GetAllAsync_Should_ReturnAllActiveRoles_When_RolesExist()
    {
        // Arrange
        using var context = CreateContext();
        var role1 = RoleBuilder.CreateDefault().WithName("Admin").Build();
        var role2 = RoleBuilder.CreateDefault().WithName("User").Build();
        var role3 = RoleBuilder.CreateDefault().WithName("Guest").Build();
        await context.Roles.AddRangeAsync(role1, role2, role3);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(r => r.Name == "Admin");
        result.Should().Contain(r => r.Name == "User");
        result.Should().Contain(r => r.Name == "Guest");
    }

    [Fact]
    public async Task GetAllAsync_Should_ReturnEmptyList_When_NoRolesExist()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_Should_ExcludeInactiveRoles()
    {
        // Arrange
        using var context = CreateContext();
        var activeRole = RoleBuilder.CreateDefault().WithName("Active").Build();
        var inactiveRole = RoleBuilder.CreateDefault().WithName("Inactive").Build();
        inactiveRole.IsActive = false;
        await context.Roles.AddRangeAsync(activeRole, inactiveRole);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(r => r.Name == "Active");
        result.Should().NotContain(r => r.Name == "Inactive");
    }

    [Fact]
    public async Task GetAllAsync_Should_ReturnRolesOrderedByName()
    {
        // Arrange
        using var context = CreateContext();
        var roleZ = RoleBuilder.CreateDefault().WithName("Zebra").Build();
        var roleA = RoleBuilder.CreateDefault().WithName("Alpha").Build();
        var roleM = RoleBuilder.CreateDefault().WithName("Middle").Build();
        await context.Roles.AddRangeAsync(roleZ, roleA, roleM);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);

        // Act
        var result = (await repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Middle");
        result[2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task AddAsync_Should_AddRole_When_ValidRole()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);
        var role = RoleBuilder.CreateDefault()
            .WithName("NewRole")
            .WithDescription("New role description")
            .Build();

        // Act
        var result = await repository.AddAsync(role);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("NewRole");

        // Verify it was saved to database
        var savedRole = await context.Roles.FindAsync(role.Id);
        savedRole.Should().NotBeNull();
        savedRole!.Name.Should().Be("NewRole");
    }

    [Fact]
    public async Task AddAsync_Should_ThrowArgumentNullException_When_RoleIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);

        // Act
        var act = async () => await repository.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("role");
    }

    [Fact]
    public async Task AddAsync_Should_PreserveRoleProperties_When_Saved()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);
        var role = RoleBuilder.CreateDefault()
            .WithName("ComplexRole")
            .WithDescription("Complex description")
            .WithRank(100)
            .AddPermissions("read:data", "write:data")
            .Build();

        // Act
        var result = await repository.AddAsync(role);

        // Assert
        result.Name.Should().Be("ComplexRole");
        result.Description.Should().Be("Complex description");
        result.Rank.Should().Be(100);
        result.Permissions.Should().HaveCount(2);
        result.Permissions.Should().Contain("read:data");
        result.Permissions.Should().Contain("write:data");
    }

    [Fact]
    public async Task AddAsync_Should_SetCreatedDate_When_RoleSaved()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new RoleRepository(context);
        var beforeAdd = DateTime.UtcNow;
        var role = RoleBuilder.CreateDefault().WithName("TimeTestRole").Build();

        // Act
        await repository.AddAsync(role);
        var afterAdd = DateTime.UtcNow;

        // Assert
        role.CreatedDate.Should().BeOnOrAfter(beforeAdd);
        role.CreatedDate.Should().BeOnOrBefore(afterAdd);
    }

    [Fact]
    public async Task GetByIdAsync_Should_SupportCancellationToken()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault().WithName("CancelTest").Build();
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await repository.GetByIdAsync(role.Id, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("CancelTest");
    }

    [Fact]
    public async Task GetAllAsync_Should_SupportCancellationToken()
    {
        // Arrange
        using var context = CreateContext();
        var role = RoleBuilder.CreateDefault().WithName("CancelTest").Build();
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await repository.GetAllAsync(cts.Token);

        // Assert
        result.Should().HaveCount(1);
    }
}
