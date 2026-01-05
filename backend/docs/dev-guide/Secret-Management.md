# Secret Management Guide

This guide covers best practices and procedures for managing secrets in the MfePortal backend application.

## Table of Contents

- [Overview](#overview)
- [Development Environment](#development-environment)
- [Azure Key Vault Integration](#azure-key-vault-integration)
- [Local Development](#local-development)
- [Secret Rotation](#secret-rotation)
- [Security Best Practices](#security-best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

Secrets management is critical for protecting sensitive information such as:
- Database connection strings
- API keys and tokens
- Service credentials
- Encryption keys

### Secret Storage Strategy

| Environment | Storage Method | Tool/Service |
|-------------|----------------|---------------|
| **Local Development** | .NET User Secrets | `dotnet user-secrets` CLI |
| **Test/Staging** | Azure Key Vault | Azure Key Vault service |
| **Production** | Azure Key Vault | Azure Key Vault service |

This application uses a clear and security-focused approach:
- **Local Development**: Use .NET User Secrets Manager (never use Key Vault locally)
- **Test & Production**: Use Azure Key Vault with managed identities
- **DAPR Integration**: DAPR secrets components access Key Vault secrets at runtime

## Development Environment

### Using User Secrets

For local development, use the .NET User Secrets Manager instead of storing secrets in configuration files.

#### Initialize User Secrets

```bash
cd backend/AugmentService/AugmentService.Api
dotnet user-secrets init
```

#### Set User Secrets

```bash
# Set a secret
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=mydb;..."

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "SecretName"
```

#### User Secrets Location

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- **Linux/Mac**: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

### Configuration Hierarchy

The application loads configuration in this order (later sources override earlier ones):

1. appsettings.json
2. appsettings.{Environment}.json
3. User Secrets (development only)
4. Environment variables
5. Command line arguments
6. DAPR secrets (via Secret Store component)

## Azure Key Vault Integration

### When to Use Azure Key Vault

Azure Key Vault should **only** be used in **Test and Production environments**. Never access Key Vault from your local development machine.

### Prerequisites

- Azure subscription with appropriate access
- Azure CLI installed and authenticated
- Managed identity with Key Vault access (for deployments)
- User access policies configured (for pipeline operations)

### Accessing Secrets from Key Vault

In **Test and Production environments**, the application automatically loads secrets from Azure Key Vault through the configuration provider:

```csharp
var keyVaultUrl = new Uri(builder.Configuration["AzureKeyVault:VaultUri"]);
var credential = new DefaultAzureCredential();
builder.Configuration.AddAzureKeyVault(keyVaultUrl, credential);
```

**Important**: This code should only execute when the environment is NOT "Development". The application configuration should conditionally add Key Vault only for Test/Production.

```csharp
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = new Uri(builder.Configuration["AzureKeyVault:VaultUri"]);
    var credential = new DefaultAzureCredential();
    builder.Configuration.AddAzureKeyVault(keyVaultUrl, credential);
}
```

### Secret Naming Convention

Use hierarchical naming with double dashes (--) for nested configuration:

```
ConnectionStrings--DefaultConnection
Authentication--ApiKey
ServiceBus--ConnectionString
Logging--LogLevel--Default
```

These map to configuration paths like:
```csharp
configuration["ConnectionStrings:DefaultConnection"]
configuration["Authentication:ApiKey"]
configuration["ServiceBus:ConnectionString"]
```

### Setting Secrets in Key Vault

```bash
# Using Azure CLI
az keyvault secret set --vault-name mykeyvault --name "ConnectionStrings--DefaultConnection" --value "your-connection-string"

# Using PowerShell
$secret = ConvertTo-SecureString -String "your-connection-string" -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName mykeyvault -Name "ConnectionStrings--DefaultConnection" -SecretValue $secret
```

## Local Development

### Important: Never Use Azure Key Vault Locally

**Do not attempt to access Azure Key Vault from your local development environment.** Instead:

- Use `.NET User Secrets` for all local secrets
- Store local configuration in `appsettings.Development.json` (for non-sensitive values only)
- Use `.env` files with `docker-compose.override.yml` for Docker-based local development
- Use DAPR local components for testing DAPR-based secret access patterns

### Docker Compose Secrets

For local development with Docker Compose, secrets are managed through:

1. **Environment files** (.env) - Add to .gitignore
2. **Docker secrets** - For swarm mode
3. **Volume mounts** - For sensitive files

### Example .env File

```env
# Database
DB_CONNECTION_STRING=Server=sqlserver;Database=mfeportal;...

# Service Bus
SERVICE_BUS_CONNECTION_STRING=Endpoint=sb://...

# API Keys
WEATHER_API_KEY=your-key-here
```

**Never commit .env files to the repository.**

### Local DAPR Secrets Component

For local testing of DAPR-based secret access, configure a local file-based secrets store in `dapr/components/secrets.yaml`:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: secretstore
  namespace: dapr
spec:
  type: secretstores.local.file
  version: v1
  metadata:
  - name: secretsPath
    value: ./dapr/secrets
```

Create a `dapr/secrets/` directory with local test secrets (do not commit to repository):

```json
{
  "database-connection": "Server=localhost;Database=testdb;...",
  "api-key": "test-key-value"
}
```

## Secret Rotation

### Applicable Environments

Secret rotation applies to **Test and Production environments only**. Rotation is managed through Azure Key Vault.

### Rotation Schedule

- **API Keys**: Every 90 days
- **Database Credentials**: Every 180 days
- **Service Principal Credentials**: Every 90 days
- **SSL/TLS Certificates**: Before expiration (typically annually)

### Rotation Process

1. **Generate new secret** in Azure Key Vault (Test/Production)
2. **Add new secret** alongside the old one (with version suffix if needed)
3. **Update application** configuration to reference the new secret name
4. **Test thoroughly** in Test environment first
5. **Deploy** to Production
6. **Verify** application is using the new secret via Key Vault logs
7. **Revoke/delete** old secret after 30-day retention period

For local development, simply update the User Secret value with `dotnet user-secrets set`.

### Rotation Monitoring

Monitor these indicators for secret rotation needs:
- Secret expiration dates in Key Vault
- External service notifications (API key expiration)
- Audit logs showing secret access patterns

## Security Best Practices

### DO's ✓

- ✓ Use managed identities for Azure services
- ✓ Enable Key Vault purge protection
- ✓ Rotate secrets regularly
- ✓ Audit and monitor secret access
- ✓ Use principle of least privilege for secret access
- ✓ Enable Key Vault soft delete and recovery
- ✓ Encrypt secrets at rest and in transit
- ✓ Use HTTPS for all external API communications
- ✓ Implement secret access logging
- ✓ Review and validate all secret references

### DON'Ts ✗

- ✗ Never commit secrets to version control
- ✗ Never log sensitive information
- ✗ Don't share secrets via email or chat
- ✗ Don't hardcode secrets in application code
- ✗ Don't grant unnecessary secret access permissions
- ✗ Don't use the same secret across environments
- ✗ Don't disable Key Vault audit logging
- ✗ Don't share secret access credentials
- ✗ Don't store backups of secrets outside Key Vault
- ✗ Don't ignore expiration warnings

### Code Review Checklist

When reviewing PRs, verify:

- [ ] No hardcoded credentials in code
- [ ] No secrets in configuration files
- [ ] Proper use of configuration providers
- [ ] No sensitive data in logs or error messages
- [ ] User Secrets used for local development
- [ ] Key Vault references use correct naming convention
- [ ] No direct secret passing in parameters
- [ ] Proper error handling without exposing secrets

### Secret Access Audit

Enable Azure Key Vault diagnostic settings to log:
- Secret access events
- Secret modifications
- Failed access attempts
- Client IP and time

```bash
# Enable auditing via CLI
az monitor diagnostic-settings create \
  --resource-group myresourcegroup \
  --resource-name mykeyvault \
  --resource-type vaults \
  --name kv-diagnostics \
  --logs '[{"category":"AuditEvent","enabled":true}]'
```

## Troubleshooting

### Secret Not Found Errors

**Problem**: `KeyVaultErrorException: Secret not found`

**Solutions**:
1. Verify secret name spelling and hierarchy (use -- for nesting)
2. Check that the identity has access to the key vault
3. Confirm the secret exists in Key Vault
4. Verify the environment-specific secret is set

### Access Denied Errors

**Problem**: `AccessDeniedException: The user does not have permission to access secrets`

**Solutions**:
1. Check access policies in Key Vault
2. Verify the managed identity or user principal
3. Ensure proper RBAC roles are assigned
4. For local development, verify User Secrets are initialized

### Local Development Issues

**Problem**: User Secrets not being read

**Solutions**:
1. Verify `dotnet user-secrets init` was run
2. Check the user-secrets-id in `.csproj` file
3. Ensure user secrets are set: `dotnet user-secrets list`
4. Clear cache: `dotnet clean` and rebuild

### DAPR Secret Access

**Problem**: DAPR cannot access secrets

**Solutions**:
1. Verify DAPR component is correctly configured
2. Check DAPR sidecar is running
3. Verify component file location and naming
4. Check DAPR logs: `dapr run --enable-debug`

### Docker Compose Issues

**Problem**: Secrets not available in container

**Solutions**:
1. Verify .env file is in the correct location
2. Check `docker-compose.yml` for environment variable configuration
3. Ensure container restart after changing secrets
4. Verify volume mounts for secret files

## Related Documentation

- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [DAPR Secrets Component](https://docs.dapr.io/reference/components-reference/supported-secret-stores/)
- [.NET Configuration Providers](https://docs.microsoft.com/dotnet/core/extensions/configuration-providers)
- [User Secrets Manager](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Managed Identities](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)

## Support

For secret management issues:
1. Check this guide and troubleshooting section
2. Review Azure Key Vault audit logs
3. Contact the security team for access issues
4. Report bugs in the development team's issue tracker
