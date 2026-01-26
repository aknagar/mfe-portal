using AugmentService.Core.Entities;

namespace AugmentService.Core.Interfaces;

/// <summary>
/// Repository interface for UserRole entity operations.
/// Handles user-role assignments and permission queries.
/// </summary>
public interface IUserRoleRepository
{
    /// <summary>
    /// Retrieves all active roles for a specific user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of roles assigned to the user.</returns>
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all unique permissions for a user by aggregating from all their roles.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Distinct collection of permission strings.</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission through any of their roles.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="permission">The permission name to check (e.g., "System.Write").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if user has the permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary role for a user (highest rank).
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role with highest rank if user has roles, null otherwise.</returns>
    Task<Role?> GetPrimaryRoleAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userRole">The user-role assignment to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added user-role assignment.</returns>
    Task<UserRole> AddAsync(UserRole userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role assignment from a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="roleId">The role's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
