using AugmentService.Core.Entities;

namespace Common.Builders;

/// <summary>
/// Fluent builder for creating Role entities in tests.
/// Provides a convenient API for building roles with valid test data.
/// </summary>
public class RoleBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = $"TestRole_{Guid.NewGuid().ToString().Substring(0, 8)}";
    private string _description = "Test role description";
    private List<string> _permissions = new() { "System.Read" };
    private int _rank = 50;
    private bool _isActive = true;
    private DateTime _createdDate = DateTime.UtcNow;
    private DateTime? _updatedDate = null;
    private List<UserRole> _userRoles = new();

    /// <summary>
    /// Sets the role ID.
    /// </summary>
    public RoleBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the role name.
    /// </summary>
    public RoleBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the role description.
    /// </summary>
    public RoleBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the permissions list.
    /// </summary>
    public RoleBuilder WithPermissions(params string[] permissions)
    {
        _permissions = permissions.ToList();
        return this;
    }

    /// <summary>
    /// Adds permissions to the existing list.
    /// </summary>
    public RoleBuilder AddPermissions(params string[] permissions)
    {
        _permissions.AddRange(permissions);
        return this;
    }

    /// <summary>
    /// Sets the role rank.
    /// </summary>
    public RoleBuilder WithRank(int rank)
    {
        _rank = rank;
        return this;
    }

    /// <summary>
    /// Sets whether the role is active.
    /// </summary>
    public RoleBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    /// <summary>
    /// Sets the created date.
    /// </summary>
    public RoleBuilder WithCreatedDate(DateTime createdDate)
    {
        _createdDate = createdDate;
        return this;
    }

    /// <summary>
    /// Sets the updated date.
    /// </summary>
    public RoleBuilder WithUpdatedDate(DateTime? updatedDate)
    {
        _updatedDate = updatedDate;
        return this;
    }

    /// <summary>
    /// Creates a Reader role (System.Read permission, rank 1).
    /// </summary>
    public static RoleBuilder CreateReader()
    {
        return new RoleBuilder()
            .WithName("Reader")
            .WithDescription("Read-only access")
            .WithPermissions("System.Read")
            .WithRank(1);
    }

    /// <summary>
    /// Creates a Writer role (System.Read, System.Write permissions, rank 50).
    /// </summary>
    public static RoleBuilder CreateWriter()
    {
        return new RoleBuilder()
            .WithName("Writer")
            .WithDescription("Read and write access")
            .WithPermissions("System.Read", "System.Write")
            .WithRank(50);
    }

    /// <summary>
    /// Creates an Admin role (System.Read, System.Write, System.Admin permissions, rank 999).
    /// </summary>
    public static RoleBuilder CreateAdmin()
    {
        return new RoleBuilder()
            .WithName("Administrator")
            .WithDescription("Full system access")
            .WithPermissions("System.Read", "System.Write", "System.Admin")
            .WithRank(999);
    }

    /// <summary>
    /// Builds the Role entity with the configured values.
    /// </summary>
    public Role Build()
    {
        return new Role
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Permissions = _permissions,
            Rank = _rank,
            IsActive = _isActive,
            CreatedDate = _createdDate,
            UpdatedDate = _updatedDate,
            UserRoles = _userRoles
        };
    }

    /// <summary>
    /// Creates a new RoleBuilder instance.
    /// </summary>
    public static RoleBuilder CreateDefault() => new();
}
