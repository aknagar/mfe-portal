using System.ComponentModel.DataAnnotations;

namespace AugmentService.Core.Entities;

/// <summary>
/// Represents the assignment of a role to a user (join table).
/// </summary>
public class UserRole
{
    /// <summary>
    /// Primary key for the user-role assignment.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to User entity.
    /// </summary>
    [Required]
    public required Guid UserId { get; set; }

    /// <summary>
    /// Foreign key to Role entity.
    /// </summary>
    [Required]
    public required Guid RoleId { get; set; }

    /// <summary>
    /// When the assignment was created (UTC).
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp (UTC, nullable).
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Navigation property to the assigned role.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user (optional for query performance).
    /// </summary>
    public User? User { get; set; }
}
