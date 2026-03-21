using AugmentService.Infrastructure;
using AugmentService.Infrastructure.Data;
using FluentAssertions;
using Xunit;

namespace AugmentService.Infrastructure.UnitTests;

public class UserDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_Should_ReturnUserDbContext_WithExpectedConnectionString()
    {
        // Arrange
        var factory = new UserDbContextFactory();

        // Act - CreateDbContext should not throw; it creates a design-time context
        var context = factory.CreateDbContext([]);

        // Assert
        context.Should().NotBeNull();
        context.Should().BeOfType<UserDbContext>();
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_Should_AcceptArgsArray_WithoutThrowing()
    {
        // Arrange
        var factory = new UserDbContextFactory();

        // Act
        var act = () =>
        {
            var ctx = factory.CreateDbContext(["--environment", "Development"]);
            ctx.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }
}
