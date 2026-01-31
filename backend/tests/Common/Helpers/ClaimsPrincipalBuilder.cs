using System.Security.Claims;

namespace Common.Helpers;

/// <summary>
/// Fluent builder for creating ClaimsPrincipal objects for controller testing.
/// Simulates authenticated users with various claims.
/// </summary>
public class ClaimsPrincipalBuilder
{
    private readonly List<Claim> _claims = new();
    private string _authenticationType = "TestAuthentication";

    /// <summary>
    /// Sets the user ID claim (NameIdentifier).
    /// </summary>
    public ClaimsPrincipalBuilder WithUserId(Guid userId)
    {
        _claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        return this;
    }

    /// <summary>
    /// Sets the sub claim (alternative user identifier).
    /// </summary>
    public ClaimsPrincipalBuilder WithSubClaim(Guid userId)
    {
        _claims.Add(new Claim("sub", userId.ToString()));
        return this;
    }

    /// <summary>
    /// Sets the custom userId claim.
    /// </summary>
    public ClaimsPrincipalBuilder WithUserIdClaim(Guid userId)
    {
        _claims.Add(new Claim("userId", userId.ToString()));
        return this;
    }

    /// <summary>
    /// Sets the email claim.
    /// </summary>
    public ClaimsPrincipalBuilder WithEmail(string email)
    {
        _claims.Add(new Claim(ClaimTypes.Email, email));
        return this;
    }

    /// <summary>
    /// Sets the name claim.
    /// </summary>
    public ClaimsPrincipalBuilder WithName(string name)
    {
        _claims.Add(new Claim(ClaimTypes.Name, name));
        return this;
    }

    /// <summary>
    /// Adds a role claim.
    /// </summary>
    public ClaimsPrincipalBuilder WithRole(string role)
    {
        _claims.Add(new Claim(ClaimTypes.Role, role));
        return this;
    }

    /// <summary>
    /// Adds multiple role claims.
    /// </summary>
    public ClaimsPrincipalBuilder WithRoles(params string[] roles)
    {
        foreach (var role in roles)
        {
            _claims.Add(new Claim(ClaimTypes.Role, role));
        }
        return this;
    }

    /// <summary>
    /// Adds a custom claim.
    /// </summary>
    public ClaimsPrincipalBuilder WithClaim(string type, string value)
    {
        _claims.Add(new Claim(type, value));
        return this;
    }

    /// <summary>
    /// Sets the authentication type.
    /// </summary>
    public ClaimsPrincipalBuilder WithAuthenticationType(string authenticationType)
    {
        _authenticationType = authenticationType;
        return this;
    }

    /// <summary>
    /// Creates a ClaimsPrincipal for an authenticated admin user.
    /// </summary>
    public static ClaimsPrincipalBuilder CreateAdminUser()
    {
        var userId = Guid.NewGuid();
        return new ClaimsPrincipalBuilder()
            .WithUserId(userId)
            .WithEmail("admin@example.com")
            .WithName("Admin User")
            .WithRole("Administrator");
    }

    /// <summary>
    /// Creates a ClaimsPrincipal for an authenticated regular user.
    /// </summary>
    public static ClaimsPrincipalBuilder CreateRegularUser()
    {
        var userId = Guid.NewGuid();
        return new ClaimsPrincipalBuilder()
            .WithUserId(userId)
            .WithEmail("user@example.com")
            .WithName("Regular User")
            .WithRole("Reader");
    }

    /// <summary>
    /// Creates a ClaimsPrincipal with no claims (unauthenticated).
    /// </summary>
    public static ClaimsPrincipalBuilder CreateUnauthenticated()
    {
        return new ClaimsPrincipalBuilder()
            .WithAuthenticationType("");
    }

    /// <summary>
    /// Builds the ClaimsPrincipal with the configured claims.
    /// </summary>
    public ClaimsPrincipal Build()
    {
        var identity = new ClaimsIdentity(_claims, _authenticationType);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates a new ClaimsPrincipalBuilder instance.
    /// </summary>
    public static ClaimsPrincipalBuilder CreateDefault() => new();
}
