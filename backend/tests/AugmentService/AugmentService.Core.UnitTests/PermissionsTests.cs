using AugmentService.Core;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.UnitTests;

public class PermissionsTests
{
    [Fact]
    public void System_Read_Should_BeCorrectValue()
    {
        Permissions.System.Read.Should().Be("System.Read");
    }

    [Fact]
    public void System_Write_Should_BeCorrectValue()
    {
        Permissions.System.Write.Should().Be("System.Write");
    }

    [Fact]
    public void System_Admin_Should_BeCorrectValue()
    {
        Permissions.System.Admin.Should().Be("System.Admin");
    }

    [Fact]
    public void Roles_Reader_Should_HaveCorrectProperties()
    {
        var reader = Permissions.Roles.Reader;
        reader.Name.Should().Be("Reader");
        reader.Rank.Should().Be(1);
        reader.IsActive.Should().BeTrue();
        reader.Permissions.Should().ContainSingle(p => p == Permissions.System.Read);
    }

    [Fact]
    public void Roles_Writer_Should_HaveCorrectProperties()
    {
        var writer = Permissions.Roles.Writer;
        writer.Name.Should().Be("Writer");
        writer.Rank.Should().Be(50);
        writer.IsActive.Should().BeTrue();
        writer.Permissions.Should().Contain(Permissions.System.Read);
        writer.Permissions.Should().Contain(Permissions.System.Write);
    }

    [Fact]
    public void Roles_Administrator_Should_HaveCorrectProperties()
    {
        var admin = Permissions.Roles.Administrator;
        admin.Name.Should().Be("Administrator");
        admin.Rank.Should().Be(999);
        admin.IsActive.Should().BeTrue();
        admin.Permissions.Should().Contain(Permissions.System.Read);
        admin.Permissions.Should().Contain(Permissions.System.Write);
        admin.Permissions.Should().Contain(Permissions.System.Admin);
    }

    [Fact]
    public void GetAllRoles_Should_ReturnThreeRoles()
    {
        var roles = Permissions.Roles.GetAllRoles().ToList();
        roles.Should().HaveCount(3);
    }

    [Fact]
    public void GetAllRoles_Should_ContainReaderWriterAdministrator()
    {
        var roles = Permissions.Roles.GetAllRoles().ToList();
        roles.Should().Contain(r => r.Name == "Reader");
        roles.Should().Contain(r => r.Name == "Writer");
        roles.Should().Contain(r => r.Name == "Administrator");
    }

    [Fact]
    public void GetAllRoles_Should_ReturnRolesWithUniqueIds()
    {
        var roles = Permissions.Roles.GetAllRoles().ToList();
        roles.Select(r => r.Id).Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void GetAllRoles_Should_BeEnumerableMultipleTimes()
    {
        var first = Permissions.Roles.GetAllRoles().ToList();
        var second = Permissions.Roles.GetAllRoles().ToList();
        first.Select(r => r.Name).Should().BeEquivalentTo(second.Select(r => r.Name));
    }

    [Fact]
    public void RoleDefinition_Should_HaveRequiredProperties()
    {
        var role = new Permissions.RoleDefinition
        {
            Id = Guid.NewGuid(), Name = "Test", Description = "Test Role",
            Permissions = new List<string> { "System.Read" },
            Rank = 10, IsActive = true
        };
        role.Name.Should().Be("Test");
        role.Rank.Should().Be(10);
    }
}
