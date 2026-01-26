using System.ComponentModel.DataAnnotations;
using AugmentService.Core.Attributes;

namespace AugmentService.Core.Entities;

/// <summary>
/// Represents a named collection of permissions that can be assigned to users.
/// </summary>
public class Role
{
    /// <summary>
    /// Primary key - unique identifier for the role.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Role name (unique) - e.g., "Reader", "Writer", "Administrator".
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// Human-readable description of the role.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Description { get; set; }

    /// <summary>
    /// List of permissions granted by this role.
    /// Stored as JSONB in PostgreSQL.
    /// Each permission follows "Resource.Action" pattern (e.g., "System.Read").
    /// </summary>
    [Required]
    [PermissionPattern]
    public required List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Role hierarchy rank (1-999).
    /// Higher rank indicates higher priority when user has multiple roles.
    /// </summary>
    [Required]
    [Range(1, 999)]
    public required int Rank { get; set; }

    /// <summary>
    /// Soft delete flag - inactive roles are ignored in permission checks.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the role was created (UTC).
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp (UTC, nullable).
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Navigation property - users assigned to this role.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
