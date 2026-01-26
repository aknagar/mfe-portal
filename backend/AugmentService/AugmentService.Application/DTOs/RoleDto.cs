namespace AugmentService.Application.DTOs;

/// <summary>
/// Role data transfer object.
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

    /// <summary>
    /// List of permissions granted by this role.
    /// Each permission follows "Resource.Action" pattern (e.g., "System.Read").
    /// </summary>
    public required List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Role hierarchy rank (1-999).
    /// Higher rank indicates higher priority when user has multiple roles.
    /// </summary>
    public required int Rank { get; set; }
}
