namespace AugmentService.Application.DTOs;

/// <summary>
/// Role data transfer object (simplified format).
/// Does not expose internal permissions or rank for security.
/// </summary>
public class RoleDto
{
    /// <summary>
    /// Unique identifier for the role.
    /// </summary>
    public required Guid RoleId { get; set; }

    /// <summary>
    /// Role name (e.g., "Reader", "Writer", "Administrator").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Human-readable role description.
    /// </summary>
    public required string Description { get; set; }
}
