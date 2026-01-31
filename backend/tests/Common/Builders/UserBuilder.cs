using AugmentService.Core.Entities;

namespace Common.Builders;

/// <summary>
/// Fluent builder for creating User entities in tests.
/// Provides a convenient API for building users with valid test data.
/// </summary>
public class UserBuilder
{
    private Guid _userId = Guid.NewGuid();
    private string _email = $"test.user.{Guid.NewGuid()}@example.com";
    private DateTime _createdDate = DateTime.UtcNow;
    private DateTime? _updatedDate = null;
    private List<UserRole> _userRoles = new();

    /// <summary>
    /// Sets the user ID.
    /// </summary>
    public UserBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Sets the user's email address.
    /// </summary>
    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>
    /// Sets the created date.
    /// </summary>
    public UserBuilder WithCreatedDate(DateTime createdDate)
    {
        _createdDate = createdDate;
        return this;
    }

    /// <summary>
    /// Sets the updated date.
    /// </summary>
    public UserBuilder WithUpdatedDate(DateTime? updatedDate)
    {
        _updatedDate = updatedDate;
        return this;
    }

    /// <summary>
    /// Adds a role to the user.
    /// </summary>
    public UserBuilder WithRole(Role role)
    {
        var userRole = new UserRole
        {
            UserId = _userId,
            RoleId = role.Id,
            Role = role
        };
        _userRoles.Add(userRole);
        return this;
    }

    /// <summary>
    /// Adds multiple roles to the user.
    /// </summary>
    public UserBuilder WithRoles(params Role[] roles)
    {
        foreach (var role in roles)
        {
            WithRole(role);
        }
        return this;
    }

    /// <summary>
    /// Builds the User entity with the configured values.
    /// </summary>
    public User Build()
    {
        var user = new User
        {
            UserId = _userId,
            Email = _email,
            CreatedDate = _createdDate,
            UpdatedDate = _updatedDate,
            UserRoles = _userRoles
        };

        // Update UserRole references
        foreach (var userRole in _userRoles)
        {
            userRole.User = user;
            userRole.UserId = user.UserId;
        }

        return user;
    }

    /// <summary>
    /// Creates a new UserBuilder instance.
    /// </summary>
    public static UserBuilder CreateDefault() => new();
}
