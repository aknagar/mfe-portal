namespace AugmentService.Core;

/// <summary>
/// Centralized permission and role definitions for the authorization system.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// System-level permission constants following "Resource.Action" pattern.
    /// </summary>
    public static class System
    {
        /// <summary>
        /// Read-only access to system resources.
        /// </summary>
        public const string Read = "System.Read";

        /// <summary>
        /// Write access to system resources.
        /// </summary>
        public const string Write = "System.Write";

        /// <summary>
        /// Administrative access to system resources.
        /// </summary>
        public const string Admin = "System.Admin";
    }

    /// <summary>
    /// Pre-defined role definitions for seeding the database.
    /// Roles are ordered by rank (lowest to highest privilege).
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Reader role: Grants read-only access (Rank: 1).
        /// </summary>
        public static readonly RoleDefinition Reader = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Reader",
            Description = "Read-only access to resources",
            Permissions = new List<string> { System.Read },
            Rank = 1,
            IsActive = true
        };

        /// <summary>
        /// Writer role: Grants read and write access (Rank: 50).
        /// </summary>
        public static readonly RoleDefinition Writer = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Name = "Writer",
            Description = "Read and write access to resources",
            Permissions = new List<string> { System.Read, System.Write },
            Rank = 50,
            IsActive = true
        };

        /// <summary>
        /// Administrator role: Grants full administrative access (Rank: 999).
        /// </summary>
        public static readonly RoleDefinition Administrator = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Name = "Administrator",
            Description = "Full administrative access",
            Permissions = new List<string> { System.Read, System.Write, System.Admin },
            Rank = 999,
            IsActive = true
        };

        /// <summary>
        /// Returns all pre-defined roles for database seeding.
        /// </summary>
        /// <returns>Enumerable collection of all role definitions.</returns>
        public static IEnumerable<RoleDefinition> GetAllRoles()
        {
            yield return Reader;
            yield return Writer;
            yield return Administrator;
        }
    }

    /// <summary>
    /// Role definition structure used for seeding and validation.
    /// Matches the Role entity structure without navigation properties.
    /// </summary>
    public class RoleDefinition
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required List<string> Permissions { get; init; }
        public required int Rank { get; init; }
        public required bool IsActive { get; init; }
    }
}
