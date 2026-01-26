using System.ComponentModel.DataAnnotations;

namespace AugmentService.Application.DTOs;

/// <summary>
/// Request DTO for checking if user has a specific permission.
/// NOTE: This class is not currently used. The CheckPermission endpoint
/// uses a path parameter instead of request body.
/// Kept for potential future use.
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

