using AugmentService.Core.Entities;
using AutoFixture;
using AutoFixture.Xunit2;
using Common.Builders;
using Common.Fixtures;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests.Entities;

/// <summary>
/// Unit tests for User entity.
/// Tests entity creation, validation, and business rules.
/// </summary>
public class UserTests
{
    private readonly IFixture _fixture;

    public UserTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new DomainCustomization());
    }

    [Fact]
    public void Should_CreateUser_When_ValidDataProvided()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var user = new User
        {
            Email = email
        };

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be(email);
        user.UserId.Should().NotBe(Guid.Empty);
        user.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Should_GenerateUserId_When_UserCreated()
    {
        // Arrange & Act
        var user1 = UserBuilder.CreateDefault().Build();
        var user2 = UserBuilder.CreateDefault().Build();

        // Assert
        user1.UserId.Should().NotBe(Guid.Empty);
        user2.UserId.Should().NotBe(Guid.Empty);
        user1.UserId.Should().NotBe(user2.UserId); // Each user should have unique ID
    }

    [Fact]
    public void Should_SetCreatedDate_When_UserCreated()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var user = UserBuilder.CreateDefault().Build();

        // Assert
        var afterCreation = DateTime.UtcNow;
        user.CreatedDate.Should().BeOnOrAfter(beforeCreation);
        user.CreatedDate.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Should_InitializeUserRolesCollection_When_UserCreated()
    {
        // Act
        var user = UserBuilder.CreateDefault().Build();

        // Assert
        user.UserRoles.Should().NotBeNull();
        user.UserRoles.Should().BeEmpty();
    }

    [Theory]
    [InlineData("valid@example.com")]
    [InlineData("test.user@company.co.uk")]
    [InlineData("admin@test-domain.org")]
    public void Should_AcceptValidEmail_When_EmailFormatIsCorrect(string validEmail)
    {
        // Act
        var user = UserBuilder.CreateDefault()
            .WithEmail(validEmail)
            .Build();

        // Assert
        user.Email.Should().Be(validEmail);
    }

    [Fact]
    public void Should_LeaveUpdatedDateNull_When_UserCreated()
    {
        // Act
        var user = UserBuilder.CreateDefault().Build();

        // Assert
        user.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public void Should_AllowSettingUpdatedDate_When_UserIsModified()
    {
        // Arrange
        var updatedDate = DateTime.UtcNow.AddMinutes(10);

        // Act
        var user = UserBuilder.CreateDefault()
            .WithUpdatedDate(updatedDate)
            .Build();

        // Assert
        user.UpdatedDate.Should().Be(updatedDate);
    }

    [Fact]
    public void Should_AllowAddingRoles_When_UserExists()
    {
        // Arrange
        var role = RoleBuilder.CreateReader().Build();

        // Act
        var user = UserBuilder.CreateDefault()
            .WithRole(role)
            .Build();

        // Assert
        user.UserRoles.Should().ContainSingle();
        user.UserRoles.First().RoleId.Should().Be(role.Id);
        user.UserRoles.First().Role.Should().Be(role);
    }

    [Fact]
    public void Should_AllowMultipleRoles_When_RolesAreAdded()
    {
        // Arrange
        var readerRole = RoleBuilder.CreateReader().Build();
        var writerRole = RoleBuilder.CreateWriter().Build();

        // Act
        var user = UserBuilder.CreateDefault()
            .WithRoles(readerRole, writerRole)
            .Build();

        // Assert
        user.UserRoles.Should().HaveCount(2);
        user.UserRoles.Select(ur => ur.RoleId).Should().Contain(new[] { readerRole.Id, writerRole.Id });
    }

    [Fact]
    public void Should_UseAutoFixture_When_GeneratingTestData()
    {
        // Act - AutoFixture with domain customization should generate a valid user
        var user = _fixture.Create<User>();

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().NotBeNullOrWhiteSpace();
        user.Email.Should().Contain("@"); // Valid email format
        user.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Should_HaveCorrectDefaultValues_When_UserCreated()
    {
        // Arrange & Act
        var user = UserBuilder.CreateDefault().Build();

        // Assert
        user.UserId.Should().NotBe(Guid.Empty);
        user.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedDate.Should().BeNull();
        user.UserRoles.Should().NotBeNull();
        user.UserRoles.Should().BeEmpty();
    }

    [Fact]
    public void Should_PreserveUserId_When_SetExplicitly()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();

        // Act
        var user = UserBuilder.CreateDefault()
            .WithUserId(expectedUserId)
            .Build();

        // Assert
        user.UserId.Should().Be(expectedUserId);
    }

    [Fact]
    public void Should_SupportCustomCreatedDate_When_SetExplicitly()
    {
        // Arrange
        var expectedDate = DateTime.UtcNow.AddDays(-7);

        // Act
        var user = UserBuilder.CreateDefault()
            .WithCreatedDate(expectedDate)
            .Build();

        // Assert
        user.CreatedDate.Should().Be(expectedDate);
    }
}
