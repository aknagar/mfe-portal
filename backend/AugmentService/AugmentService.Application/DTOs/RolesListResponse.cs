namespace AugmentService.Application.DTOs;

/// <summary>
/// Response DTO containing list of all available roles.
/// </summary>
public class RolesListResponse
{
    /// <summary>
    /// List of all active roles in the system.
    /// </summary>
    public required List<RoleDto> Roles { get; set; } = new();
}
