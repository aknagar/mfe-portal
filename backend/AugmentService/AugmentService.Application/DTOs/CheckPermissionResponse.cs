namespace AugmentService.Application.DTOs;

/// <summary>
/// Response DTO for permission check endpoint.
/// Indicates whether the user has a specific permission.
/// </summary>
public class CheckPermissionResponse
{
    /// <summary>
    /// The permission name that was checked.
    /// </summary>
    public required string Permission { get; init; }

    /// <summary>
    /// True if the user has the permission, false otherwise.
    /// </summary>
    public required bool HasPermission { get; init; }
}
