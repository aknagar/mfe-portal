using System.ComponentModel.DataAnnotations;

namespace AugmentService.Application.DTOs;

/// <summary>
/// Request DTO for checking if user has a specific permission.
/// </summary>
public class CheckPermissionRequest
{
    /// <summary>
    /// Permission name to check (case-sensitive).
    /// Must follow "Resource.Action" pattern (e.g., "System.Write").
    /// </summary>
    [Required(ErrorMessage = "Permission field is required")]
    [RegularExpression(@"^[A-Za-z]+\.[A-Za-z]+$", ErrorMessage = "Permission must follow 'Resource.Action' pattern")]
    public required string Permission { get; set; }
}

/// <summary>
/// Response DTO for permission check result.
/// </summary>
public class CheckPermissionResponse
{
    /// <summary>
    /// The permission that was checked.
    /// </summary>
    public required string Permission { get; set; }

    /// <summary>
    /// True if user has the permission, false otherwise.
    /// </summary>
    public required bool HasPermission { get; set; }
}
