using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AugmentService.Core.Attributes;

/// <summary>
/// Validates that a permission string follows the "Resource.Action" pattern
/// Examples: System.Read, System.Write, Augment.Admin
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public partial class PermissionPatternAttribute : ValidationAttribute
{
    private const string PermissionPattern = @"^[A-Za-z]+\.[A-Za-z]+$";

    [GeneratedRegex(PermissionPattern, RegexOptions.Compiled)]
    private static partial Regex PermissionRegex();

    public PermissionPatternAttribute() 
        : base("Permission must follow the 'Resource.Action' pattern (e.g., System.Read, System.Write)")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Use [Required] to enforce non-null
        }

        if (value is string permission)
        {
            if (!PermissionRegex().IsMatch(permission))
            {
                return new ValidationResult(
                    $"Permission '{permission}' does not follow the 'Resource.Action' pattern. Expected format: 'Resource.Action' (e.g., System.Read)");
            }

            return ValidationResult.Success;
        }

        if (value is IEnumerable<string> permissions)
        {
            var invalidPermissions = permissions
                .Where(p => !PermissionRegex().IsMatch(p))
                .ToList();

            if (invalidPermissions.Count != 0)
            {
                return new ValidationResult(
                    $"The following permissions do not follow the 'Resource.Action' pattern: {string.Join(", ", invalidPermissions)}. Expected format: 'Resource.Action' (e.g., System.Read)");
            }

            return ValidationResult.Success;
        }

        return new ValidationResult("Permission must be a string or a collection of strings");
    }
}
