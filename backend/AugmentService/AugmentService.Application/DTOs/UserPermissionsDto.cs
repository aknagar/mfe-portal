namespace AugmentService.Application.DTOs;

/// <summary>
/// Response DTO containing user's roles and aggregated permissions.
/// Simplified format with role names only (no rank or permissions per role).
/// </summary>
public class UserPermissionsDto
{
    /// <summary>
    /// Unique identifier for the authenticated user.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Email address of the authenticated user from Azure AD token.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// List of role names assigned to the user (simplified - just names).
    /// </summary>
    public required List<string> Roles { get; set; } = new();

    /// <summary>
    /// Primary role name (highest ranked role).
    /// Null if user has no roles assigned.
    /// </summary>
    public string? PrimaryRole { get; set; }

    /// <summary>
    /// Aggregated unique permissions from all roles.
    /// </summary>
    public required List<string> Permissions { get; set; } = new();
}
