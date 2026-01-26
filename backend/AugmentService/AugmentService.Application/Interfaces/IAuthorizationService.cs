using AugmentService.Application.DTOs;

namespace AugmentService.Application.Interfaces;

/// <summary>
/// Service interface for user authorization and permission management.
/// Note: Named IPermissionService to avoid conflict with ASP.NET Core's IAuthorizationService.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Retrieves all roles and aggregated permissions for a specific user.
    /// Results are cached for the session duration.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User permissions DTO with roles and aggregated permissions.</returns>
    Task<UserPermissionsDto> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission.
    /// Leverages cached permissions for performance.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="permission">The permission name to check (e.g., "System.Write").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if user has the permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active roles in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of all active roles with their permissions.</returns>
    Task<IEnumerable<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);
}
