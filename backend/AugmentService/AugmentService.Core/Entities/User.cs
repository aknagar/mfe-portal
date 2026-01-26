using System.ComponentModel.DataAnnotations;

namespace AugmentService.Core.Entities;

/// <summary>
/// Represents an authenticated user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Primary key - unique identifier for the user.
    /// </summary>
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User's email address (unique).
    /// </summary>
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// When the user was created (UTC).
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp (UTC, nullable).
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Navigation property - roles assigned to this user.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
