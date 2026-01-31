using AugmentService.Core.Entities;
using AutoFixture;
using Common.Builders;
using Common.Fixtures;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests.Entities;

/// <summary>
/// Unit tests for Role entity.
/// Tests role creation, permissions management, and validation rules.
/// </summary>
public class RoleTests
{
    private readonly IFixture _fixture;

    public RoleTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new DomainCustomization());
    }

    [Fact]
    public void Should_CreateRole_When_ValidDataProvided()
    {
        // Arrange
        var name = "TestRole";
        var description = "Test role description";
        var permissions = new List<string> { "System.Read", "System.Write" };
        var rank = 50;

        // Act
        var role = new Role
        {
            Name = name,
            Description = description,
            Permissions = permissions,
            Rank = rank
        };

        // Assert
        role.Should().NotBeNull();
        role.Name.Should().Be(name);
        role.Description.Should().Be(description);
        role.Permissions.Should().BeEquivalentTo(permissions);
        role.Rank.Should().Be(rank);
    }

    [Fact]
    public void Should_GenerateRoleId_When_RoleCreated()
    {
        // Act
        var role1 = RoleBuilder.CreateDefault().Build();
        var role2 = RoleBuilder.CreateDefault().Build();

        // Assert
        role1.Id.Should().NotBe(Guid.Empty);
        role2.Id.Should().NotBe(Guid.Empty);
        role1.Id.Should().NotBe(role2.Id);
    }

    [Fact]
    public void Should_InitializePermissionsList_When_RoleCreated()
    {
        // Act
        var role = RoleBuilder.CreateDefault().Build();

        // Assert
        role.Permissions.Should().NotBeNull();
        role.Permissions.Should().NotBeEmpty(); // Default builder has System.Read
    }

    [Fact]
    public void Should_SetIsActiveToTrue_When_RoleCreated()
    {
        // Act
        var role = RoleBuilder.CreateDefault().Build();

        // Assert
        role.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Should_AllowAddingPermissions_When_RoleExists()
    {
        // Arrange
        var role = RoleBuilder.CreateDefault()
            .WithPermissions("System.Read")
            .Build();

        // Act
        role.Permissions.Add("System.Write");

        // Assert
        role.Permissions.Should().Contain(new[] { "System.Read", "System.Write" });
    }

    [Fact]
    public void Should_AllowRemovingPermissions_When_RoleExists()
    {
        // Arrange
        var role = RoleBuilder.CreateDefault()
            .WithPermissions("System.Read", "System.Write", "System.Admin")
            .Build();

        // Act
        role.Permissions.Remove("System.Admin");

        // Assert
        role.Permissions.Should().NotContain("System.Admin");
        role.Permissions.Should().Contain(new[] { "System.Read", "System.Write" });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(999)]
    public void Should_AcceptValidRank_When_RankInRange(int rank)
    {
        // Act
        var role = RoleBuilder.CreateDefault()
            .WithRank(rank)
            .Build();

        // Assert
        role.Rank.Should().Be(rank);
    }

    [Fact]
    public void Should_CreateReaderRole_When_UsingReaderBuilder()
    {
        // Act
        var role = RoleBuilder.CreateReader().Build();

        // Assert
        role.Name.Should().Be("Reader");
        role.Description.Should().Be("Read-only access");
        role.Permissions.Should().Contain("System.Read");
        role.Rank.Should().Be(1);
    }

    [Fact]
    public void Should_CreateWriterRole_When_UsingWriterBuilder()
    {
        // Act
        var role = RoleBuilder.CreateWriter().Build();

        // Assert
        role.Name.Should().Be("Writer");
        role.Description.Should().Be("Read and write access");
        role.Permissions.Should().Contain(new[] { "System.Read", "System.Write" });
        role.Rank.Should().Be(50);
    }

    [Fact]
    public void Should_CreateAdminRole_When_UsingAdminBuilder()
    {
        // Act
        var role = RoleBuilder.CreateAdmin().Build();

        // Assert
        role.Name.Should().Be("Administrator");
        role.Description.Should().Be("Full system access");
        role.Permissions.Should().Contain(new[] { "System.Read", "System.Write", "System.Admin" });
        role.Rank.Should().Be(999);
    }

    [Fact]
    public void Should_AllowDeactivatingRole_When_IsActiveSetToFalse()
    {
        // Arrange
        var role = RoleBuilder.CreateDefault()
            .WithIsActive(false)
            .Build();

        // Act & Assert
        role.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Should_SetCreatedDate_When_RoleCreated()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var role = RoleBuilder.CreateDefault().Build();

        // Assert
        var afterCreation = DateTime.UtcNow;
        role.CreatedDate.Should().BeOnOrAfter(beforeCreation);
        role.CreatedDate.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Should_LeaveUpdatedDateNull_When_RoleCreated()
    {
        // Act
        var role = RoleBuilder.CreateDefault().Build();

        // Assert
        role.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public void Should_SupportCustomDescription_When_SetExplicitly()
    {
        // Arrange
        var customDescription = "Custom role for testing";

        // Act
        var role = RoleBuilder.CreateDefault()
            .WithDescription(customDescription)
            .Build();

        // Assert
        role.Description.Should().Be(customDescription);
    }

    [Fact]
    public void Should_UseAutoFixture_When_GeneratingTestData()
    {
        // Act
        var role = _fixture.Create<Role>();

        // Assert
        role.Should().NotBeNull();
        role.Name.Should().NotBeNullOrWhiteSpace();
        role.Description.Should().NotBeNullOrWhiteSpace();
        role.Permissions.Should().NotBeNull();
        role.Rank.Should().BeInRange(1, 999);
    }

    [Fact]
    public void Should_AllowFluentPermissionConfiguration_When_UsingBuilder()
    {
        // Act
        var role = RoleBuilder.CreateDefault()
            .WithPermissions("System.Read")
            .AddPermissions("System.Write", "System.Delete")
            .Build();

        // Assert
        role.Permissions.Should().HaveCount(3);
        role.Permissions.Should().Contain(new[] { "System.Read", "System.Write", "System.Delete" });
    }
}
