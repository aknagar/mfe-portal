using AugmentService.Application.DTOs;
using AugmentService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AugmentService.Api.Controllers;

/// <summary>
/// Controller for user authorization and permission management.
/// All endpoints require JWT Bearer authentication.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserPermissionService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserPermissionService userService,
        ILogger<UserController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves the current user's roles and permissions.
    /// </summary>
    /// <returns>User permissions including all roles and aggregated permissions.</returns>
    /// <response code="200">Successfully retrieved user permissions.</response>
    /// <response code="401">User is not authenticated or JWT token is invalid/expired.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("my-permissions")]
    [ProducesResponseType(typeof(UserPermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Unable to extract user ID from JWT claims");
                return Unauthorized(new { error = "Unauthorized", message = "Authentication required", details = "User ID not found in JWT claims" });
            }

            _logger.LogInformation("User {UserId} requesting their permissions", userId);

            var permissions = await _userService.GetUserPermissionsAsync(userId, cancellationToken);

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for current user");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalServerError", message = "An unexpected error occurred", details = ex.Message });
        }
    }

    /// <summary>
    /// Checks if the current user has a specific permission.
    /// </summary>
    /// <param name="permission">Permission name to check (e.g., "System.Write").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the user has the permission.</returns>
    /// <response code="200">Permission check completed successfully.</response>
    /// <response code="400">Invalid request - permission parameter is malformed or doesn't match pattern.</response>
    /// <response code="401">User is not authenticated or JWT token is invalid/expired.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("my-permissions/{permission}")]
    [ProducesResponseType(typeof(CheckPermissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckPermission(string permission, CancellationToken cancellationToken)
    {
        // Validate permission parameter format (Resource.Action pattern)
        if (string.IsNullOrWhiteSpace(permission) || !System.Text.RegularExpressions.Regex.IsMatch(permission, @"^[A-Za-z]+\.[A-Za-z]+$"))
        {
            return BadRequest(new
            {
                error = "BadRequest",
                message = "Invalid permission format",
                details = "Permission must follow 'Resource.Action' pattern (e.g., 'System.Write')"
            });
        }

        try
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Unable to extract user ID from JWT claims");
                return Unauthorized(new { error = "Unauthorized", message = "Authentication required", details = "User ID not found in JWT claims" });
            }

            _logger.LogDebug("User {UserId} checking permission {Permission}", userId, permission);

            var hasPermission = await _userService.HasPermissionAsync(userId, permission, cancellationToken);

            var response = new CheckPermissionResponse
            {
                Permission = permission,
                HasPermission = hasPermission
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for current user", permission);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalServerError", message = "An unexpected error occurred", details = ex.Message });
        }
    }

    /// <summary>
    /// Lists all available roles in the system (Admin only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all active roles with their permissions.</returns>
    /// <response code="200">Successfully retrieved all roles.</response>
    /// <response code="401">User is not authenticated or JWT token is invalid/expired.</response>
    /// <response code="403">User does not have Admin permission.</response>
    /// <response code="500">Internal server error occurred.</response>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(RolesListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Unable to extract user ID from JWT claims");
                return Unauthorized(new { error = "Unauthorized", message = "Authentication required", details = "User ID not found in JWT claims" });
            }

            // Check if user has Admin permission
            var hasAdminPermission = await _userService.HasPermissionAsync(userId, "System.Admin", cancellationToken);
            if (!hasAdminPermission)
            {
                _logger.LogWarning("User {UserId} attempted to access roles list without Admin permission", userId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Forbidden", message = "Insufficient permissions", details = "Admin permission required to access this resource" });
            }

            _logger.LogInformation("Admin user {UserId} requesting all roles", userId);

            var roles = await _userService.GetAllRolesAsync(cancellationToken);

            var response = new RolesListResponse
            {
                Roles = roles.ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all roles");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "InternalServerError", message = "An unexpected error occurred", details = ex.Message });
        }
    }

    /// <summary>
    /// Extracts user ID from JWT claims.
    /// </summary>
    /// <returns>User ID as Guid, or Guid.Empty if not found or invalid.</returns>
    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Guid.Empty;
        }

        return userId;
    }
}
