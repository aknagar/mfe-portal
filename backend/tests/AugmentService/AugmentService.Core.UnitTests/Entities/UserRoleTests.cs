using AugmentService.Core.Entities;
using AutoFixture;
using Common.Fixtures;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests.Entities;

public class UserRoleTests
{
    private readonly IFixture _fixture;

    public UserRoleTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new DomainCustomization());
    }

    [Fact]
    public void Should_CreateUserRole_When_ValidDataProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        // Act
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };

        // Assert
        userRole.Should().NotBeNull();
        userRole.UserId.Should().Be(userId);
        userRole.RoleId.Should().Be(roleId);
    }

    [Fact]
    public void Should_GenerateUniqueId_When_UserRoleCreated()
    {
        // Arrange & Act
        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        // Assert
        userRole.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_GenerateDifferentIds_When_MultipleUserRolesCreated()
    {
        // Arrange & Act
        var userRole1 = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var userRole2 = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        // Assert
        userRole1.Id.Should().NotBe(userRole2.Id);
    }

    [Fact]
    public void Should_SetCreatedDateToUtcNow_When_UserRoleCreated()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var after = DateTime.UtcNow;

        // Assert
        userRole.CreatedDate.Should().BeOnOrAfter(before);
        userRole.CreatedDate.Should().BeOnOrBefore(after);
        userRole.CreatedDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Should_SetUpdatedDateToNull_When_UserRoleCreated()
    {
        // Arrange & Act
        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        // Assert
        userRole.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public void Should_AllowUpdatedDateToBeSet_When_UserRoleModified()
    {
        // Arrange
        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var updateTime = DateTime.UtcNow.AddMinutes(10);

        // Act
        userRole.UpdatedDate = updateTime;

        // Assert
        userRole.UpdatedDate.Should().Be(updateTime);
    }

    [Fact]
    public void Should_AssignSameRoleToMultipleUsers_When_DifferentUserIdsProvided()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        // Act
        var userRole1 = new UserRole
        {
            UserId = userId1,
            RoleId = roleId
        };

        var userRole2 = new UserRole
        {
            UserId = userId2,
            RoleId = roleId
        };

        // Assert
        userRole1.RoleId.Should().Be(roleId);
        userRole2.RoleId.Should().Be(roleId);
        userRole1.UserId.Should().NotBe(userRole2.UserId);
    }

    [Fact]
    public void Should_AssignMultipleRolesToSameUser_When_DifferentRoleIdsProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        // Act
        var userRole1 = new UserRole
        {
            UserId = userId,
            RoleId = roleId1
        };

        var userRole2 = new UserRole
        {
            UserId = userId,
            RoleId = roleId2
        };

        // Assert
        userRole1.UserId.Should().Be(userId);
        userRole2.UserId.Should().Be(userId);
        userRole1.RoleId.Should().NotBe(userRole2.RoleId);
    }

    [Fact]
    public void Should_AllowExplicitIdSetting_When_UserRoleCreated()
    {
        // Arrange
        var customId = Guid.NewGuid();

        // Act
        var userRole = new UserRole
        {
            Id = customId,
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        // Assert
        userRole.Id.Should().Be(customId);
    }

    [Fact]
    public void Should_AllowExplicitCreatedDateSetting_When_UserRoleCreated()
    {
        // Arrange
        var customDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            CreatedDate = customDate
        };

        // Assert
        userRole.CreatedDate.Should().Be(customDate);
    }

    [Fact]
    public void Should_CreateUserRoleWithAutoFixture_When_DomainCustomizationUsed()
    {
        // Arrange & Act
        var userRole = _fixture.Create<UserRole>();

        // Assert
        userRole.Should().NotBeNull();
        userRole.Id.Should().NotBeEmpty();
        userRole.UserId.Should().NotBeEmpty();
        userRole.RoleId.Should().NotBeEmpty();
        userRole.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        userRole.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public void Should_CreateMultipleUniqueUserRoles_When_AutoFixtureUsed()
    {
        // Arrange & Act
        var userRole1 = _fixture.Create<UserRole>();
        var userRole2 = _fixture.Create<UserRole>();

        // Assert
        userRole1.Should().NotBeNull();
        userRole2.Should().NotBeNull();
        userRole1.Id.Should().NotBe(userRole2.Id);
        userRole1.UserId.Should().NotBe(userRole2.UserId);
        userRole1.RoleId.Should().NotBe(userRole2.RoleId);
    }

    [Fact]
    public void Should_AllowNavigationPropertyToBeSet_When_RoleProvided()
    {
        // Arrange
        var role = new Role
        {
            Name = "Admin",
            Description = "Administrator Role",
            Permissions = new List<string> { "System.Admin" },
            Rank = 999
        };

        // Act
        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = role.Id,
            Role = role
        };

        // Assert
        userRole.Role.Should().NotBeNull();
        userRole.Role.Name.Should().Be("Admin");
    }

    [Fact]
    public void Should_AllowUserNavigationPropertyToBeSet_When_UserProvided()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com"
        };

        // Act
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = Guid.NewGuid(),
            User = user
        };

        // Assert
        userRole.User.Should().NotBeNull();
        userRole.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void Should_AllowUserNavigationPropertyToBeNull_When_NotProvided()
    {
        // Arrange & Act
        var userRole = new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        // Assert
        userRole.User.Should().BeNull();
    }
}
