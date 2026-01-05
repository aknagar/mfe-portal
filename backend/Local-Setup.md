# Local Development Setup

## Prerequisites

- .NET 10.0.x
- Docker & Docker Compose
- Azure Developer CLI (azd)
- PostgreSQL (or use Docker Compose)

## Setup Steps

### 1. Configure PostgreSQL User Secrets

Navigate to the AppHost project and set the database password:

```bash
cd backend/MfePortal.AppHost
dotnet user-secrets set "Postgres:Password" "your-secure-password"
```

**Secrets location:**
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\{ProjectUserSecretsId}\secrets.json`
- **Linux/Mac:** `~/.microsoft/usersecrets/{ProjectUserSecretsId}/secrets.json`

### 2. Run Aspire Orchestration

```bash
cd backend/MfePortal.AppHost
dotnet run
```

This starts:
- PostgreSQL database
- AugmentService API
- Dashboard for monitoring

### 3. Access Services

- **Aspire Dashboard:** http://localhost:18888
- **AugmentService API:** http://localhost:5000
- **PostgreSQL:** localhost:5432

## Common Issues

| Issue | Solution |
|-------|----------|
| Port already in use | Kill existing dotnet processes: `Stop-Process -Name dotnet -Force` |
| Database connection error | Verify PostgreSQL is running and password is set correctly |
| Secrets not loading | Ensure you're in `MfePortal.AppHost` directory when setting secrets |

## Useful Commands

```bash
# Clean build
dotnet clean
dotnet build

# View secrets
dotnet user-secrets list

# Reset secrets
dotnet user-secrets clear
```
