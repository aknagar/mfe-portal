# Aspire Configuration Convention

.NET Aspire uses a **convention-based pattern** to automatically discover and inject resource URIs into services via the {ResourceName}:Uri configuration key.

## How Annotations Distinguish Between Emulators and Real Resources

| Aspect | Emulator | Real Azure Service |
|--------|----------|-------------------|
| **ContainerImageAnnotation** | ✅ Present | ❌ Absent |
| **EndpointAnnotation.Host** | `localhost` | `*.vault.azure.net` |
| **Port** | Custom (8200) | Standard (443) |
| **Config Source** | `appsettings.Development.json` | Environment variables |

### Detection of IsContainer()

```csharp
public static bool IsContainer(this IResource resource)
{
    return resource.Annotations
        .OfType<ContainerImageAnnotation>()
        .Any();
}
```

**Same Resource Type, Different Annotations:**

```csharp
// Both are AzureKeyVaultResource
var emulatorVault = builder.AddAzureKeyVault("keyvault");
var realVault = builder.AddAzureKeyVault("keyvault");

// Same resource type, different annotations:

// Emulator has:
// - ContainerImageAnnotation { Image = "mcr.microsoft.com/azure-key-vault:latest" }
// - EndpointAnnotation { Host = "localhost", Port = 8200 }

// Real Azure has:
// - No ContainerImageAnnotation (it's managed Azure)
// - EndpointAnnotation { Host = "myvault.vault.azure.net", Port = 443 }
```

## Aspire Integration Types

| Type | Purpose | Used In | Packages |
|------|---------|---------|----------|
| **Hosting** | Orchestrate & manage resources | AppHost Program.cs | \Aspire.Hosting.*\ |
| **Client** | Enable service connections | Service projects | \Aspire.*.Client.*\ |

**Hosting (AppHost):**
\\\csharp
var postgres = builder.AddPostgres(\"postgres\");
var keyvault = builder.AddAzureKeyVault(\"keyvault\");
\\\

**Client (Services):**
\\\csharp
builder.AddNpgsqlDbContext<AppDbContext>(\"postgresdb\");
builder.AddAzureKeyVaultClient(\"keyvault\");
\\\

## Configuration Keys

Aspire maps resource names to configuration using: \{ResourceName}:Uri\ or \{ResourceName}:ConnectionString\

\\\csharp
var keyVault = builder.AddAzureKeyVault(\"keyvault\");
// Looks for: Keyvault:Uri
\\\

### Colon (:\) Separator

The colon represents nesting in configuration:

| Flat Key | JSON Structure |
|----------|----------------|
| \Keyvault:Uri\ | \{ \"Keyvault\": { \"Uri\": \"...\" } }\ |
| \Database:ConnectionString\ | \{ \"Database\": { \"ConnectionString\": \"...\" } }\ |
| \Logging:LogLevel:Default\ | \{ \"Logging\": { \"LogLevel\": { \"Default\": \"...\" } } }\ |

### Configuration Sources (Priority Order)
1. **Environment Variables** - \ASPIRE_KEYVAULT_URI\ (highest)
2. **appsettings.json** - \Keyvault:Uri\
3. **User Secrets** - Development only
4. **Command-line Arguments** - Overrides all

## Common Resources

| Resource | Method | Config Key | Example |
|----------|--------|-----------|---------|
| Azure Key Vault | \AddAzureKeyVault(\"keyvault\")\ | \Keyvault:Uri\ | \https://myvault.vault.azure.net\ |
| PostgreSQL | \AddPostgres(\"postgres\")\ | \Postgres:ConnectionString\ | \Host=localhost;Port=5432;...\ |
| Redis | \AddRedis(\"cache\")\ | \Redis:ConnectionString\ | \localhost:6379\ |
| RabbitMQ | \AddRabbitMQ(\"messaging\")\ | \RabbitMQ:Uri\ | \mqp://localhost:5672\ |

## Configuration Examples

**appsettings.Development.json:**
\\\json
{
  \"Keyvault\": { \"Uri\": \"https://localhost:8200\" },
  \"Postgres\": { \"ConnectionString\": \"Host=localhost;Port=5432;Database=mydb;...\" }
}
\\\

**Environment Variables (Production):**
\\\ash
ASPIRE_KEYVAULT_URI=https://myvault.vault.azure.net
ASPIRE_POSTGRES_CONNECTIONSTRING=Host=prod-db;Port=5432;...
\\\

## Accessing Configuration

\\\csharp
// Via IConfiguration
var uri = config[\"Keyvault:Uri\"];

// Via environment variables
var uri = Environment.GetEnvironmentVariable(\"ASPIRE_KEYVAULT_URI\");

// Via Aspire client injection
public class MyService
{
    public MyService(SecretClient secretClient) { }
}
\\\

## Best Practices

1. **Descriptive names** - \postgres\, \
edis\, \keyvault\ (not \db\, \
\)
2. **Environment-specific configs** - Use \ppsettings.{Environment}.json\ and env vars
3. **Never commit secrets** - Use \.gitignore\, user secrets, or Azure Key Vault
4. **Document requirements** - Add comments for emulator setup
5. **Consistent naming** - Keep patterns across all resources

## Development vs. Production

**AppHost (same code for both):**
\\\csharp
var keyVault = builder.AddAzureKeyVault(\"keyvault\");
builder.AddProject<Projects.Api>(\"api\").WithReference(keyVault);
\\\

| Environment | Config Source | URI |
|-------------|---|---|
| **Development** | \ppsettings.Development.json\ → \Keyvault:Uri\ | \https://localhost:8200\ (emulator) |
| **Production** | \ASPIRE_KEYVAULT_URI\ env var | \https://prod-vault.vault.azure.net\ (real Azure) |

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Config not found | Verify \{ResourceName}:Uri\ format. Check env vars. |
| Wrong URI | Check env var overrides. Verify \WithReference()\ in AppHost. |
| Works locally, fails in production | Use env-specific appsettings. Override via CI/CD. |

## Reference

- **Docs:** https://learn.microsoft.com/en-us/dotnet/aspire/
- **GitHub:** https://github.com/dotnet/aspire
- **NuGet:** Search for \Aspire.*\ packages
