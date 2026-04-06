using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Encodings.Web;

namespace Common.Auth;

/// <summary>
/// Constants shared between the test auth handler and the token factory.
/// </summary>
public static class TestAuthConstants
{
    /// <summary>A fixed issuer that matches the AzureAd:Instance+TenantId convention used in tests.</summary>
    public const string Issuer = "https://login.microsoftonline.com/test-tenant-id/v2.0";

    /// <summary>Audience that matches AzureAd:Audience used in tests.</summary>
    public const string Audience = "api://test-client-id";

    /// <summary>Symmetric key used to sign and validate test tokens (32 bytes = 256 bit).</summary>
    public const string SigningKey = "test-integration-signing-key-32b!";

    /// <summary>AzureAd config values that the WebApplicationFactory injects so that
    /// AddMicrosoftIdentityWebApiAuthentication picks up the right issuer/audience,
    /// then the handler swap replaces the Entra ID JWT validator with a local one.</summary>
    public static readonly Dictionary<string, string?> ConfigOverrides = new()
    {
        ["AzureAd:Instance"]  = "https://login.microsoftonline.com/",
        ["AzureAd:TenantId"]  = "test-tenant-id",
        ["AzureAd:ClientId"]  = "test-client-id",
        ["AzureAd:Audience"]  = Audience,
    };
}

/// <summary>
/// Replaces the real Entra ID JWT handler in integration tests.
///
/// Why this approach instead of disabling auth entirely:
///   - Production uses Microsoft.Identity.Web which validates issuer, audience, expiry
///     and signature. This handler validates the same properties using a locally-signed
///     HS256 token, keeping the test environment as close to production as possible.
///   - Tests that forget to attach a token still get 401 — just like production.
///   - Token claims (roles, scopes, oid) flow through to HttpContext.User — so
///     authorization policies are also exercised, not bypassed.
///
/// Usage in WebApplicationFactory.ConfigureWebHost:
/// <code>
///   services.ReplaceWithTestJwtHandler();
/// </code>
/// Then obtain tokens via <see cref="TestTokenFactory"/>.
/// </summary>
public static class TestAuthServiceExtensions
{
    /// <summary>
    /// Removes the real JwtBearer handler registered by Microsoft.Identity.Web and
    /// replaces it with a test handler that validates locally-signed HS256 tokens.
    /// </summary>
    public static IServiceCollection ReplaceWithTestJwtHandler(this IServiceCollection services)
    {
        // Remove the options configuration that Microsoft.Identity.Web registered
        // (it points at Entra ID's discovery endpoint — unusable in tests).
        var jwtOptionsDescriptors = services
            .Where(d => d.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>) ||
                        d.ServiceType == typeof(IPostConfigureOptions<JwtBearerOptions>))
            .ToList();

        foreach (var d in jwtOptionsDescriptors)
            services.Remove(d);

        // Re-configure JwtBearer to use a local symmetric key.
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidIssuer              = TestAuthConstants.Issuer,
                ValidateAudience         = true,
                ValidAudience            = TestAuthConstants.Audience,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(TestAuthConstants.SigningKey)),
                ClockSkew = TimeSpan.Zero,
            };
        });

        return services;
    }
}
