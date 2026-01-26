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
/// Integration tests for RoleRepository using TestContainers PostgreSQL.
/// Tests actual database interactions with JSONB permissions.
/// </summary>
public class RoleRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private AuthorizationDbContext _context = null!;
    private RoleRepository _repository = null!;

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

        _repository = new RoleRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllAsync_ReturnsSeededRoles()
    {
        // Act
        var roles = await _repository.GetAllAsync();

        // Assert
        roles.Should().NotBeNull();
        roles.Should().HaveCount(3); // Reader, Writer, Administrator
        roles.Should().OnlyContain(r => r.IsActive);
        roles.Should().BeInAscendingOrder(r => r.Name);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByNameAsync_WithExistingRole_ReturnsCorrectRole()
    {
        // Act
        var role = await _repository.GetByNameAsync("Reader");

        // Assert
        role.Should().NotBeNull();
        role!.Name.Should().Be("Reader");
        role.Description.Should().Be("Read-only access to resources");
        role.Permissions.Should().NotBeNull();
        role.Permissions.Should().ContainSingle();
        role.Permissions.Should().Contain("System.Read");
        role.Rank.Should().Be(1);
        role.IsActive.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByNameAsync_WithNonExistentRole_ReturnsNull()
    {
        // Act
        var role = await _repository.GetByNameAsync("NonExistent");

        // Assert
        role.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByIdAsync_WithExistingRole_ReturnsCorrectRole()
    {
        // Arrange
        var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Act
        var role = await _repository.GetByIdAsync(readerRoleId);

        // Assert
        role.Should().NotBeNull();
        role!.Id.Should().Be(readerRoleId);
        role.Name.Should().Be("Reader");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddAsync_WithNewRole_AddsSuccessfully()
    {
        // Arrange
        var newRole = new Role
        {
            Name = "Reviewer",
            Description = "Review access",
            Permissions = new List<string> { "System.Read", "Review.Approve" },
            Rank = 25,
            IsActive = true
        };

        // Act
        var result = await _repository.AddAsync(newRole);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Reviewer");
        result.Permissions.Should().HaveCount(2);

        // Verify it's actually in the database
        var retrieved = await _repository.GetByIdAsync(result.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Permissions.Should().Contain("Review.Approve");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllAsync_OrderedByName_ReturnsAlphabeticallySortedRoles()
    {
        // Act
        var roles = await _repository.GetAllAsync();
        var roleNames = roles.Select(r => r.Name).ToList();

        // Assert
        roleNames.Should().BeInAscendingOrder();
        roleNames.First().Should().Be("Administrator");
        roleNames.Last().Should().Be("Writer");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByNameAsync_WithNullOrEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var actNull = async () => await _repository.GetByNameAsync(null!);
        await actNull.Should().ThrowAsync<ArgumentException>();

        var actEmpty = async () => await _repository.GetByNameAsync(string.Empty);
        await actEmpty.Should().ThrowAsync<ArgumentException>();
    }
}
