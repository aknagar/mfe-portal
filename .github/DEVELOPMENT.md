# Development Guidelines

## Security - Never Commit Secrets

Protecting sensitive information is critical. Follow these guidelines to prevent accidental secret commits.

### ✅ DO:

- Store passwords in `.env.local` or `docker-compose.override.yml` (both git-ignored)
- Use `dotnet user-secrets` for development credentials
- Use Azure Key Vault for production secrets
- Reference environment variables: `${VAR_NAME}` in configuration files
- **Store API keys in `.env.local` or user-secrets for local development** - never hardcode in source
- **Treat all authentication tokens as secrets** - rotate immediately if exposed
- Rotate credentials immediately if accidentally exposed

### ❌ DON'T:

- Hardcode passwords in configuration files (appsettings.json, docker-compose.yml, etc.)
- **Commit connection strings with credentials** (Server=host;User=user;Password=secret;)
- **Include API keys in code** (app keys, access tokens, bearer tokens, subscription keys)
- Store database passwords in comments or documentation
- Add secrets to environment variables committed to git
- Upload private keys or certificates to the repository
- Include authentication tokens or session secrets
- Commit OAuth tokens, JWT secrets, or signing keys

## If You Accidentally Committed Secrets

### Immediate Actions:

1. **Rotate the credentials immediately** - assume they are compromised
   ```powershell
   # For Azure resources
   az keyvault secret set --vault-name your-vault --name SecretName --value newvalue
   ```

2. **Remove from git history** using git-filter-repo:
   ```powershell
   # Install git-filter-repo
   pip install git-filter-repo

   # Remove file(s) containing secrets from entire history
   git filter-repo --path backend/appsettings.json --invert-paths
   git filter-repo --path backend/appsettings.Development.json --invert-paths

   # Force push (⚠️ only if repo is private and team is notified)
   git push origin --force-with-lease
   ```

4. **Scan git history** to ensure no other secrets exist:
   ```powershell
   pip install detect-secrets
   detect-secrets scan --all-files
   ```

## Pre-commit Checks

Install git-secrets to automatically prevent commits with secrets:

```powershell
# Install git-secrets (requires Chocolatey)
choco install git-secrets

# Initialize for this repository
git secrets --install

# Add patterns to detect
git secrets --add '(password|pwd|secret|token|api.?key|apikey|access.?key)'
git secrets --add '(AWS_SECRET|AZURE_KEY|DATABASE_PASSWORD)'
```

After installation, git will warn you before committing files with secret patterns.

## What Counts as a Secret?

**Always treat the following as secrets:**

- 🔐 **Database credentials**: Passwords, connection strings with passwords
- 🔐 **API keys**: Third-party service keys, subscription keys, app keys
- 🔐 **Tokens**: JWT secrets, OAuth tokens, bearer tokens, session tokens
- 🔐 **Authentication**: OAuth secrets, private keys, certificates, signing keys
- 🔐 **Cloud access**: connection strings
- 🔐 **Internal services**: Internal API keys, service tokens, webhook secrets

**Examples of secrets to NEVER commit:**
```csharp
// ❌ WRONG
var connectionString = "Server=db.azure.com;User=admin;Password=MySecretPassword123!;";
var apiKey = "sk-1234567890abcdefghijklmnop";
var jwtSecret = "super-secret-jwt-signing-key";
var cosmosKey = "Primary Key from Azure Portal";
```

**Always use placeholders:**
```csharp
// ✅ CORRECT
var connectionString = "${ConnectionString}";
var apiKey = configuration["ApiKeys:ExternalService"];
var jwtSecret = configuration.GetValue<string>("JwtSecret");
```

## Secrets Storage by Environment

| Environment | Storage Method | Examples |
|---|---|---|
| **Local Dev** | `.env.local`, `docker-compose.override.yml`, user-secrets | DB passwords, API keys, JWT secrets |
| **CI/CD** | GitHub Secrets / Azure DevOps Secrets | Repository secrets, deployment credentials |
| **Staging** | Azure Key Vault + Managed Identity | Connection strings, API keys, certificates |
| **Production** | Azure Key Vault + Managed Identity | All secrets retrieved via managed identity |

## References

- [Microsoft: Protect secrets in development](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [OWASP: Secret Management](https://owasp.org/www-community/Sensitive_Data_Exposure)
- [detect-secrets Documentation](https://github.com/Yelp/detect-secrets)
- [git-secrets](https://github.com/awslabs/git-secrets)
