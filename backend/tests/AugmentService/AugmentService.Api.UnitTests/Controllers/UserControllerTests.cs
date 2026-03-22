using AugmentService.Api.Controllers;
using AugmentService.Application.DTOs;
using AugmentService.Application.Interfaces;
using Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AugmentService.Api.UnitTests.Controllers;

public class UserControllerTests
{
    private readonly IUserPermissionService _userService;
    private readonly ILogger<UserController> _logger;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _userService = Substitute.For<IUserPermissionService>();
        _logger = Substitute.For<ILogger<UserController>>();
        _controller = new UserController(_userService, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Should_ThrowArgumentNullException_When_UserServiceIsNull()
    {
        // Act
        var act = () => new UserController(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("userService");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Act
        var act = () => new UserController(_userService, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetMyPermissions Tests

    [Fact]
    public async Task Should_ReturnOkWithPermissions_When_GetMyPermissionsCalled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateDefault()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var permissions = new UserPermissionsDto
        {
            UserId = userId,
            Email = "test@example.com",
            Roles = new List<string> { "Reader" },
            Permissions = new List<string> { "System.Read" },
            PrimaryRole = "Reader"
        };
        _userService.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(permissions);

        // Act
        var result = await _controller.GetMyPermissions(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIdNotInClaims()
    {
        // Arrange - User with no claims
        var user = ClaimsPrincipalBuilder.CreateUnauthenticated().Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.GetMyPermissions(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Should_Return500_When_ServiceThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateDefault()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _userService.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMyPermissions(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Should_CallServiceWithCorrectUserId_When_GetMyPermissionsCalled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateDefault()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var permissions = new UserPermissionsDto
        {
            UserId = userId,
            Email = "test@example.com",
            Roles = new List<string>(),
            Permissions = new List<string>()
        };
        _userService.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(permissions);

        // Act
        await _controller.GetMyPermissions(CancellationToken.None);

        // Assert
        await _userService.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region CheckPermission Tests

    [Fact]
    public async Task Should_ReturnOkWithHasPermissionTrue_When_UserHasPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateDefault()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _userService.HasPermissionAsync(userId, "System.Write", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _controller.CheckPermission("System.Write", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as CheckPermissionResponse;
        response.Should().NotBeNull();
        response!.Permission.Should().Be("System.Write");
        response.HasPermission.Should().BeTrue();
    }

    [Fact]
    public async Task Should_ReturnOkWithHasPermissionFalse_When_UserDoesNotHavePermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateDefault()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _userService.HasPermissionAsync(userId, "System.Admin", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _controller.CheckPermission("System.Admin", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as CheckPermissionResponse;
        response.Should().NotBeNull();
        response!.HasPermission.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("InvalidFormat")]
    [InlineData("System")]
    [InlineData("System.Write.Extra")]
    public async Task Should_ReturnBadRequest_When_PermissionNameIsInvalid(string invalidPermission)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateDefault()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.CheckPermission(invalidPermission, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIdNotInClaimsForCheckPermission()
    {
        // Arrange
        var user = ClaimsPrincipalBuilder.CreateUnauthenticated().Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.CheckPermission("System.Read", CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Should_Return500_When_ServiceThrowsExceptionInCheckPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateDefault()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _userService.HasPermissionAsync(userId, "System.Read", Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.CheckPermission("System.Read", CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion

    #region GetAllRoles Tests

    [Fact]
    public async Task Should_ReturnOkWithRoles_When_UserHasAdminPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipalBuilder()
            .WithUserId(userId)
            .WithEmail("admin@example.com")
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _userService.HasPermissionAsync(userId, "System.Admin", Arg.Any<CancellationToken>())
            .Returns(true);

        var roles = new List<RoleDto>
        {
            new RoleDto { RoleId = Guid.NewGuid(), Name = "Reader", Description = "Read-only" },
            new RoleDto { RoleId = Guid.NewGuid(), Name = "Writer", Description = "Read-write" }
        };
        _userService.GetAllRolesAsync(Arg.Any<CancellationToken>())
            .Returns(roles);

        // Act
        var result = await _controller.GetAllRoles(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as RolesListResponse;
        response.Should().NotBeNull();
        response!.Roles.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_UserDoesNotHaveAdminPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateRegularUser()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _userService.HasPermissionAsync(userId, "System.Admin", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _controller.GetAllRoles(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIdNotInClaimsForGetAllRoles()
    {
        // Arrange
        var user = ClaimsPrincipalBuilder.CreateUnauthenticated().Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.GetAllRoles(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Should_Return500_When_ServiceThrowsExceptionInGetAllRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipalBuilder()
            .WithUserId(userId)
            .WithEmail("admin@example.com")
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _userService.HasPermissionAsync(userId, "System.Admin", Arg.Any<CancellationToken>())
            .Returns(true);
        _userService.GetAllRolesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllRoles(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Should_NotCallGetAllRolesAsync_When_UserLacksAdminPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = ClaimsPrincipalBuilder.CreateRegularUser()
            .WithUserId(userId)
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _userService.HasPermissionAsync(userId, "System.Admin", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _controller.GetAllRoles(CancellationToken.None);

        // Assert
        await _userService.DidNotReceive().GetAllRolesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region User ID Extraction Tests

    [Theory]
    [InlineData("sub")]
    [InlineData("userId")]
    public async Task Should_ExtractUserId_When_UsingDifferentClaimTypes(string claimType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipalBuilder()
            .WithClaim(claimType, userId.ToString())
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var permissions = new UserPermissionsDto
        {
            UserId = userId,
            Email = "test@example.com",
            Roles = new List<string>(),
            Permissions = new List<string>()
        };
        _userService.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(permissions);

        // Act
        var result = await _controller.GetMyPermissions(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        await _userService.Received(1).GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIdClaimIsInvalidGuid()
    {
        // Arrange
        var user = new ClaimsPrincipalBuilder()
            .WithClaim("sub", "not-a-guid")
            .Build();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.GetMyPermissions(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion
}
