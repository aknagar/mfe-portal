namespace AugmentService.Application.DTOs;

/// <summary>
/// Response DTO containing user's roles and aggregated permissions.
/// </summary>
public class UserPermissionsDto
{
    /// <summary>
    /// Unique identifier for the authenticated user.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// List of roles assigned to the user, ordered by rank (highest first).
    /// </summary>
    public required List<RoleDto> Roles { get; set; } = new();

    /// <summary>
    /// Aggregated unique permissions from all roles.
    /// </summary>
    public required List<string> Permissions { get; set; } = new();
}
