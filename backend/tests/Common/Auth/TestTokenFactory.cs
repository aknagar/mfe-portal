using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Common.Auth;

/// <summary>
/// Generates locally-signed JWT Bearer tokens for integration tests.
///
/// The tokens are structurally identical to real Entra ID tokens
/// (same claims shape, same validation rules) but signed with a
/// known symmetric key instead of Entra ID's RSA keys.
///
/// Example usage:
/// <code>
/// var token = TestTokenFactory.CreateToken();
/// client.DefaultRequestHeaders.Authorization =
///     new AuthenticationHeaderValue("Bearer", token);
/// </code>
/// </summary>
public static class TestTokenFactory
{
    private static readonly SymmetricSecurityKey _key =
        new(Encoding.UTF8.GetBytes(TestAuthConstants.SigningKey));

    private static readonly SigningCredentials _credentials =
        new(_key, SecurityAlgorithms.HmacSha256);

    /// <summary>
    /// Creates a valid Bearer token with the supplied identity claims.
    /// Defaults to a regular authenticated user if no claims are specified.
    /// </summary>
    public static string CreateToken(
        string? userId    = null,
        string? email     = null,
        string? name      = null,
        string[]? roles   = null,
        string[]? scopes  = null,
        TimeSpan? expiry  = null)
    {
        var claims = new List<Claim>
        {
            new("oid",  userId ?? Guid.NewGuid().ToString()),
            new("sub",  userId ?? Guid.NewGuid().ToString()),
            new(ClaimTypes.Email,            email ?? "[email protected]"),
            new(ClaimTypes.Name,             name  ?? "Test User"),
            new("preferred_username",        email ?? "[email protected]"),
        };

        foreach (var role in roles ?? [])
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var scope in scopes ?? [])
            claims.Add(new Claim("scp", scope));

        var token = new JwtSecurityToken(
            issuer:             TestAuthConstants.Issuer,
            audience:           TestAuthConstants.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow + (expiry ?? TimeSpan.FromHours(1)),
            signingCredentials: _credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Creates a token for an admin user with the "Admin" role.</summary>
    public static string CreateAdminToken(string? userId = null) =>
        CreateToken(userId: userId, email: "[email protected]", name: "Admin User", roles: ["Admin"]);

    /// <summary>Creates a token that is already expired (issued 2 h ago, expired 1 h ago).</summary>
    public static string CreateExpiredToken()
    {
        var claims = new List<Claim>
        {
            new("oid",  Guid.NewGuid().ToString()),
            new("sub",  Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "[email protected]"),
            new(ClaimTypes.Name,  "Expired User"),
        };

        var token = new JwtSecurityToken(
            issuer:             TestAuthConstants.Issuer,
            audience:           TestAuthConstants.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow.AddHours(-2),   // issued 2h ago
            expires:            DateTime.UtcNow.AddHours(-1),   // expired 1h ago
            signingCredentials: _credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Returns an <see cref="System.Net.Http.Headers.AuthenticationHeaderValue"/>
    /// ready to set on <see cref="System.Net.Http.HttpClient.DefaultRequestHeaders"/>.
    /// </summary>
    public static System.Net.Http.Headers.AuthenticationHeaderValue CreateBearerHeader(
        string? userId   = null,
        string? email    = null,
        string? name     = null,
        string[]? roles  = null,
        string[]? scopes = null) =>
        new("Bearer", CreateToken(userId, email, name, roles, scopes));
}
