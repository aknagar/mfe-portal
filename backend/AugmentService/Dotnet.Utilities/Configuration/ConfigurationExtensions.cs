using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Identity;

namespace Dotnet.Utilities.Configuration;

/// <summary>
/// Extension methods for configuring the application with the recommended configuration hierarchy.
/// 
/// Implements the configuration hierarchy as defined in Configuration-Management.md:
/// 1. appsettings.json (base/default configuration)
/// 2. appsettings.{Environment}.json (environment-specific overrides)
/// 3. Azure Key Vault (if KeyVault:Url is configured)
/// 4. User Secrets (local development only)
/// 5. Environment Variables (system-level overrides)
/// 6. Command-line Arguments (CLI overrides)
/// </summary>
public static class ConfigurationExtensions
{
    public static WebApplicationBuilder AddConfiguration(
        this WebApplicationBuilder builder,
        string? basePath = null)
    {
        builder.AddJsonFiles(basePath)
               .AddAzureKeyVaultSecrets()
               .AddUserSecretsForLocalDev()  // Add user secrets for local development
               .AddEnvironmentVariables();

        return builder;
    }

    public static WebApplicationBuilder AddJsonFiles(
        this WebApplicationBuilder builder,
        string? basePath = null)
    {
        basePath ??= Directory.GetCurrentDirectory();

        builder.Configuration
            .AddJsonFile(
                Path.Combine(basePath, "appsettings.json"),
                optional: false,
                reloadOnChange: !builder.Environment.IsProduction());

        // Add environment-specific configuration file (optional)
        var environmentFile = $"appsettings.{builder.Environment.EnvironmentName}.json";
        builder.Configuration
            .AddJsonFile(
                Path.Combine(basePath, environmentFile),
                optional: true,
                reloadOnChange: !builder.Environment.IsProduction());

        return builder;
    }

    public static WebApplicationBuilder AddUserSecretsForLocalDev(
       this WebApplicationBuilder builder)
    {
        // Add user secrets only in Development
        if (builder.Environment.IsDevelopment())
        {
            // Add user secrets from the calling assembly
            try
            {
                builder.Configuration.AddUserSecrets(typeof(ConfigurationExtensions).Assembly, optional: true);
            }
            catch
            {
                // User secrets may not be initialized; continue without them
            }
        }

        return builder;
    }

    public static WebApplicationBuilder AddAzureKeyVaultSecrets(
        this WebApplicationBuilder builder)
    {
        var keyVaultUrl = builder.Configuration["KeyVault:Url"];

        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            return builder;
        }

        try
        {
            var credential = new DefaultAzureCredential();
            builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
        }
        catch (Exception ex)
        {
            // Log warning but don't fail startup if Key Vault is unavailable
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to load Azure Key Vault configuration: {ex.Message}");
        }

        return builder;
    }

    public static WebApplicationBuilder AddEnvironmentVariables(
        this WebApplicationBuilder builder)
    {
        // Add environment variables (supports __ for hierarchy)
        builder.Configuration.AddEnvironmentVariables();
        return builder;
    }

    /// <summary>
    /// Validates that required configuration values are present in the WebApplicationBuilder.
    /// Throws InvalidOperationException if any required configuration is missing.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance.</param>
    /// <param name="requiredKeys">Array of required configuration keys using colon notation (e.g., "ConnectionStrings:DefaultConnection").</param>
    /// <returns>The WebApplicationBuilder for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if any required configuration key is missing or null.</exception>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddConfiguration()
    ///        .AddConfigurationValidator(
    ///            "ConnectionStrings:DefaultConnection",
    ///            "KeyVault:Url");
    /// </code>
    /// </example>
    public static WebApplicationBuilder AddConfigurationValidator(
        this WebApplicationBuilder builder,
        params string[] requiredKeys)
    {
        builder.Configuration.AddConfigurationValidator(requiredKeys);
        return builder;
    }

    /// <summary>
    /// Validates that required configuration values are present.
    /// Throws InvalidOperationException if any required configuration is missing.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance.</param>
    /// <param name="requiredKeys">Array of required configuration keys using colon notation (e.g., "ConnectionStrings:DefaultConnection").</param>
    /// <returns>The configuration instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if any required configuration key is missing or null.</exception>
    /// <example>
    /// <code>
    /// builder.Configuration
    ///     .AddConfigurationValidator(
    ///         "ConnectionStrings:DefaultConnection",
    ///         "KeyVault:Url",
    ///         "Features:EnableCaching");
    /// </code>
    /// </example>
    public static IConfiguration AddConfigurationValidator(
        this IConfiguration configuration,
        params string[] requiredKeys)
    {
        var missingKeys = requiredKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToList();

        if (missingKeys.Count > 0)
        {
            var keys = string.Join(", ", missingKeys.Select(k => $"'{k}'"));
            throw new InvalidOperationException(
                $"Required configuration values are missing: {keys}. " +
                "Ensure these values are configured in appsettings.json, environment-specific files, " +
                "user secrets (Development), or environment variables.");
        }

        return configuration;
    }
}
