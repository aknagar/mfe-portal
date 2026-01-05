# Configuration Management Guide

This guide covers configuration management strategies, best practices, and procedures for the MfePortal backend application.

## Table of Contents

- [Overview](#overview)
- [Configuration Hierarchy](#configuration-hierarchy)
- [Environment-Specific Configuration](#environment-specific-configuration)
- [Configuration Files](#configuration-files)
- [Configuration Providers](#configuration-providers)
- [DAPR Configuration](#dapr-configuration)
- [Application Settings](#application-settings)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

Configuration management ensures consistent application behavior across development, staging, and production environments while maintaining flexibility and security.

The application uses a **configuration-as-code** approach with:
- JSON-based configuration files
- Environment variable overrides
- DAPR component configuration
- Azure Key Vault for secrets

## Configuration Hierarchy

Configuration is loaded in this specific order, with later sources overriding earlier ones:

1. **appsettings.json** (base/default configuration)
2. **appsettings.{Environment}.json** (environment-specific overrides)
3. **Azure Key Vault** (if KeyVault:Url is configured)
4. **User Secrets** (local development only)
5. **Environment Variables** (system-level overrides)
6. **Command-line Arguments** (CLI overrides)

### Example Precedence

If `ConnectionString` is defined in multiple sources:

```
appsettings.json:
  "ConnectionString": "dev-db"

appsettings.Production.json:
  "ConnectionString": "prod-db"

Azure Key Vault:
  ConnectionString = "keyvault-db"

Environment Variable:
  ConnectionString = "staging-db"
```

**Result**: The environment variable value (`staging-db`) takes precedence. However, Key Vault takes precedence over User Secrets in shared environments.

## Environment-Specific Configuration

### Supported Environments

The application supports three main environments:

| Environment | Purpose | Configuration File |
|-------------|---------|-------------------|
| Development | Local development | appsettings.Development.json |
| Staging | Pre-production testing | appsettings.Staging.json |
| Production | Production deployment | appsettings.json (base) |

### Setting the Environment

#### In Code

```csharp
var environment = builder.Environment.EnvironmentName;
// Returns: "Development", "Staging", or "Production"
```

#### At Runtime

**Windows Command Line**:
```bash
set ASPNETCORE_ENVIRONMENT=Production
dotnet run
```

**PowerShell**:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet run
```

**Linux/Mac**:
```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet run
```

#### Docker

```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
```

#### Docker Compose

```yaml
services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
```

## Configuration Files

### appsettings.json Structure

The base configuration file contains default settings applicable to all environments:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=mfeportal;..."
  },
  "AzureKeyVault": {
    "VaultUri": "https://mykeyvault.vault.azure.net/"
  },
  "ServiceBus": {
    "HostName": "mymessagebus.servicebus.windows.net",
    "QueueName": "default-queue"
  },
  "Features": {
    "EnableCaching": true,
    "CacheDurationSeconds": 3600
  }
}
```

### Environment-Specific Files

Create overrides for specific environments:

**appsettings.Development.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug"
    }
  },
  "Features": {
    "EnableCaching": false
  }
}
```

**appsettings.Production.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error"
    }
  },
  "Features": {
    "CacheDurationSeconds": 7200
  }
}
```

### Configuration File Best Practices

- ✓ Use PascalCase for JSON keys
- ✓ Use hierarchical structure for organization
- ✓ Keep base `appsettings.json` with defaults
- ✓ Only override what differs in environment files
- ✓ Never commit secrets to any configuration file
- ✓ Use clear, descriptive section names
- ✓ Add comments for complex configuration
- ✓ Validate configuration on startup

## Configuration Providers

### Built-in Providers

The application uses these configuration providers in order:

#### JSON File Provider

```csharp
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
```

**Properties**:
- `optional`: Allow missing files
- `reloadOnChange`: Reload when file changes (development only)

#### Azure Key Vault Provider

**Loaded BEFORE User Secrets to ensure Key Vault values take precedence in shared environments.**

```csharp
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    var credential = new DefaultAzureCredential();
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}
```

**Configuration**:
- Requires `KeyVault:Url` to be set in appsettings.json or environment variables
- Uses `DefaultAzureCredential` for managed identity authentication
- Gracefully handles unavailable Key Vault (logs warning, continues startup)
- Automatically loaded via `builder.AddConfiguration()` extension method

**Key Naming Convention**: Use `--` (double dash) for hierarchy in Key Vault secret names
```
Example: ConnectionStrings--DefaultConnection
Maps to: ConnectionStrings:DefaultConnection
```

#### User Secrets Provider

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}
```

**Location**: Specified in `.csproj` via `<UserSecretsId>`

**Note**: Overridden by Key Vault values, so Key Vault can enforce production settings even in development

#### Environment Variables Provider

```csharp
builder.Configuration.AddEnvironmentVariables();
```

**Format**: Use `__` (double underscore) for hierarchy
```
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=...
SERVICEBUS__HOSTNAME=mybus.servicebus.windows.net
```

**Note**: Environment variables take precedence over all other sources

### Custom Configuration Provider

Create custom providers for external configuration sources:

```csharp
public class CustomConfigurationProvider : ConfigurationProvider
{
    private readonly string _source;

    public CustomConfigurationProvider(string source) => _source = source;

    public override void Load()
    {
        // Load from custom source
        var data = new Dictionary<string, string>
        {
            { "CustomKey", "CustomValue" }
        };
        Data = data;
    }
}

// Register in Program.cs
builder.Configuration.AddProvider(new CustomConfigurationProvider("custom-source"));
```

### Using AddConfiguration() Extension Method

The `Dotnet.Utilities.Configuration` namespace provides a convenient extension method that sets up the entire configuration hierarchy automatically:

```csharp
using Dotnet.Utilities.Configuration;

var builder = WebApplication.CreateBuilder(args);

// This single call sets up the complete configuration hierarchy:
// 1. appsettings.json (required)
// 2. appsettings.{Environment}.json (optional)
// 3. Azure Key Vault (if KeyVault:Url configured)
// 4. User Secrets (Development only)
// 5. Environment Variables
// 6. Command-line arguments
builder.AddConfiguration();

// Validate required configuration
builder.Configuration.ValidateRequiredConfiguration(
    "ConnectionStrings:ProductsContext"
);

// Access configuration values safely
var optional = builder.Configuration.GetOptionalValue("Feature:ExperimentalApi");
var typed = builder.Configuration.GetConfigurationValue("Timeout", defaultValue: 30);
```

## DAPR Configuration

### DAPR Components Configuration

DAPR uses YAML component files for configuration:

**dapr/components/config.yaml**:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: daprConfig
spec:
  tracing:
    samplingRate: "1"
    zipkin:
      endpointAddress: http://zipkin:9411/api/v1/spans
  mtls:
    enabled: true
    allowedClients:
      - client1
      - client2
```

### DAPR Service Invocation Configuration

**dapr/components/service-invocation.yaml**:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: myserviceinvocation
spec:
  type: serviceInvocation.http
  version: v1
  metadata:
  - name: timeout
    value: 30s
```

### Accessing DAPR Configuration

```csharp
// Invoke a DAPR service
var result = await daprClient.InvokeMethodAsync<WeatherForecast>(
    "weather-service",
    "GetForecast"
);
```

## Application Settings

### Logging Configuration

Control logging levels per namespace:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Debug",
      "AugmentService": "Information"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```

### Feature Flags

Manage feature toggles via configuration:

```json
{
  "Features": {
    "EnableNewDashboard": false,
    "EnableBetaApi": true,
    "MaxRetries": 3
  }
}
```

Access in code:

```csharp
public class MyService
{
    private readonly IConfiguration _config;

    public MyService(IConfiguration config)
    {
        _config = config;
    }

    public void DoSomething()
    {
        if (_config.GetValue<bool>("Features:EnableNewDashboard"))
        {
            // Use new dashboard
        }
    }
}
```

### Options Pattern

Use the Options pattern for type-safe configuration:

```csharp
// Configuration class
public class WeatherApiOptions
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
    public int TimeoutSeconds { get; set; }
}

// Register in DI
builder.Services
    .Configure<WeatherApiOptions>(
        builder.Configuration.GetSection("WeatherApi")
    );

// Use in service
public class WeatherService
{
    private readonly WeatherApiOptions _options;

    public WeatherService(IOptions<WeatherApiOptions> options)
    {
        _options = options.Value;
    }
}
```

Configuration section in JSON:
```json
{
  "WeatherApi": {
    "ApiKey": "your-key",
    "BaseUrl": "https://api.weather.example.com",
    "TimeoutSeconds": 30
  }
}
```

## Best Practices

### Configuration Design

- ✓ Keep configuration externalized from code
- ✓ Use environment-specific files only for overrides
- ✓ Organize configuration hierarchically
- ✓ Use the Options pattern for complex settings
- ✓ Validate configuration on application startup
- ✓ Document all configuration sections
- ✓ Use meaningful, consistent naming conventions
- ✓ Avoid magic strings in code

### Security

- ✓ Never commit secrets to configuration files
- ✓ Use Azure Key Vault for sensitive data in shared environments
- ✓ Key Vault values override User Secrets to enforce production settings
- ✓ Use User Secrets for local development only
- ✓ Restrict environment variable access
- ✓ Audit configuration changes
- ✓ Use managed identities for Azure services
- ✓ Encrypt sensitive configuration at rest
- ✗ Don't log configuration values containing secrets
- ✗ Don't rely on User Secrets for production-critical values

### Performance

- ✓ Use `reloadOnChange: false` in production
- ✓ Cache configuration values appropriately
- ✓ Minimize external configuration calls
- ✓ Use lazy loading for large configurations
- ✓ Monitor configuration load times

### Maintenance

- ✓ Document all configuration options
- ✓ Validate configuration schema
- ✓ Version configuration with application
- ✓ Create migration guides for configuration changes
- ✓ Keep configuration DRY (Don't Repeat Yourself)
- ✓ Review configuration regularly
- ✓ Remove deprecated configuration keys
- ✓ Maintain backward compatibility when possible

### Configuration Validation

Validate configuration on startup:

```csharp
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured"
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);
```

## Troubleshooting

### Configuration Not Being Loaded

**Problem**: Configuration value returns null or default

**Solutions**:
1. Verify configuration key path and casing
2. Check if configuration file is in correct location
3. Ensure file is not excluded from build output
4. Verify environment-specific file for current environment
5. Check configuration provider registration order
6. Use debugger to inspect configuration dictionary

### Environment-Specific Configuration Not Applied

**Problem**: Environment-specific settings are ignored

**Solutions**:
1. Verify `ASPNETCORE_ENVIRONMENT` variable is set
2. Check environment file naming: `appsettings.{Environment}.json`
3. Ensure file is marked "Copy if newer" in project
4. Verify configuration provider order
5. Check for casing issues in environment names

### User Secrets Not Working

**Problem**: User Secrets are not loaded

**Solutions**:
1. Verify environment is "Development"
2. Check `<UserSecretsId>` in `.csproj`
3. Run `dotnet user-secrets init` if needed
4. Verify secrets exist: `dotnet user-secrets list`
5. Rebuild and restart application
6. Check configuration provider registration

### Azure Key Vault Access Issues

**Problem**: Cannot load configuration from Key Vault

**Solutions**:
1. Verify `KeyVault:Url` is set in appsettings.json or environment variables
2. Check managed identity or current user has `Get` and `List` permissions on secrets
3. Verify secret names follow naming convention (`--` for hierarchy)
   - Example: `ConnectionStrings--DefaultConnection`
4. Review Key Vault access policies in Azure Portal
5. Check RBAC role assignments (typically "Key Vault Secrets User")
6. Verify network connectivity to Key Vault
7. Review Azure authentication context and managed identity settings
8. Check application logs for Key Vault connection errors (non-blocking warnings)

### Docker Configuration Issues

**Problem**: Configuration not available in container

**Solutions**:
1. Verify environment variables in Dockerfile or compose file
2. Check volume mounts for configuration files
3. Verify file permissions in container
4. Check if configuration files are included in Docker build
5. Review environment variable naming (use `__` for hierarchy)

### Performance Issues

**Problem**: Application startup is slow due to configuration

**Solutions**:
1. Disable `reloadOnChange` in production
2. Reduce number of configuration providers
3. Cache configuration values
4. Defer loading of optional configuration
5. Profile configuration loading time

## Related Documentation

- [ASP.NET Core Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/configuration)
- [Options Pattern](https://docs.microsoft.com/dotnet/core/extensions/options)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.azurekeyvault)
- [Environment Variables in .NET](https://docs.microsoft.com/dotnet/core/extensions/configuration-providers#environment-variables)
- [DAPR Configuration](https://docs.dapr.io/concepts/configuration-concept/)

## Support

For configuration management issues:
1. Check this guide and troubleshooting section
2. Review application logs for configuration errors
3. Validate configuration files with JSON schema validators
4. Check Azure Key Vault audit logs
5. Report issues in the development team's issue tracker
