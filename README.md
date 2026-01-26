# MfePortal Backend

This solution contains the backend services for the MfePortal application, orchestrated by .NET Aspire.

## Projects

- **AugmentService**: A microservice that acts as a reverse proxy. It takes a URL as input, makes a request to it, logs the response, and returns the result.
  - **Authorization API**: Provides role-based access control (RBAC) with JWT authentication, PostgreSQL JSONB permission storage, and session-scoped caching for sub-200ms permission retrieval.
- **MfePortal.AppHost**: The .NET Aspire orchestrator project.
- **MfePortal.ServiceDefaults**: Shared service configurations (OpenTelemetry, Health Checks, etc.).

## Prerequisites

- .NET 10 SDK
- Docker and Docker Compose (for local PostgreSQL)
- Azure CLI (for Azure deployment)

## Database Setup

The backend uses PostgreSQL for data persistence. Two separate databases are used:
- **weather**: Stores weather forecast data
- **products**: Stores product catalog data

### Local Development with Docker

1. Start PostgreSQL using docker-compose:
   ```bash
   cd backend
   docker-compose up -d
   ```

2. Verify PostgreSQL is running:
   ```bash
   docker ps | grep postgres
   ```

3. PostgreSQL connection details for local development:
   - **Host**: localhost
   - **Port**: 5432
   - **Username**: mfeportal
   - **Password**: Set via `POSTGRES_PASSWORD` environment variable or `.env` file (never hardcode)
   - **Databases**: weather, products

4. Apply database migrations:
   ```bash
   cd backend/infra/migrations
   psql -h localhost -U mfeportal -d weather -f 001_initial_schema.sql
   psql -h localhost -U mfeportal -d weather -f 002_add_indexes.sql
   psql -h localhost -U mfeportal -d products -f 001_initial_schema.sql
   psql -h localhost -U mfeportal -d products -f 002_add_indexes.sql
   ```

5. Or use a PostgreSQL client GUI (pgAdmin, DBeaver, etc.) to connect and execute the migration scripts.

### Azure Deployment

PostgreSQL is automatically provisioned via Azure Bicep templates with:
- Azure Database for PostgreSQL Flexible Server
- Automatic connection strings stored in Azure Key Vault
- Firewall rules configured for Container App access
- Managed identity authentication for Key Vault access

Connection strings are retrieved from Key Vault at runtime using `DefaultAzureCredential`.

## How to Run

1. Ensure you have the .NET Aspire workload installed:
   ```bash
   dotnet workload install aspire
   ```

2. Run the AppHost project:
   ```bash
   dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj
   ```

3. The Aspire Dashboard will open (typically at https://localhost:15001). You can see the `augmentservice` running there.

4. You can test the AugmentService proxy endpoint:
   ```
   GET /proxy?url=https://example.com
   ```

## Authorization API

The AugmentService includes a comprehensive authorization system with role-based permissions:

### Features
- **JWT Bearer Authentication**: Secure user identification from token claims
- **Role-Based Access Control (RBAC)**: Three predefined roles (Reader, Writer, Administrator)
- **JSONB Permission Storage**: Flexible permissions stored as PostgreSQL JSONB with GIN index
- **Session-Scoped Caching**: IMemoryCache with 8-hour absolute and 30-minute sliding expiration
- **Performance**: Sub-200ms permission retrieval (first call), sub-5ms cached calls

### Endpoints

#### GET /api/authorization/my-permissions
Returns the authenticated user's complete permission set with roles and rank.

**Response:**
```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "roles": [
    {
      "roleId": "789e4567-e89b-12d3-a456-426614174000",
      "name": "Reader",
      "description": "Read-only access to system resources",
      "permissions": ["System.Read"],
      "rank": 1
    }
  ],
  "permissions": ["System.Read"]
}
```

#### POST /api/authorization/check-permission
Check if the authenticated user has a specific permission.

**Request:**
```json
{
  "permission": "System.Write"
}
```

**Response:**
```json
{
  "hasPermission": false
}
```

#### GET /api/authorization/roles
**[Admin Only]** Returns all system roles with permissions.

**Response:**
```json
{
  "roles": [
    {
      "roleId": "...",
      "name": "Reader",
      "description": "Read-only access",
      "permissions": ["System.Read"],
      "rank": 1
    },
    {
      "roleId": "...",
      "name": "Writer",
      "description": "Read and write access",
      "permissions": ["System.Read", "System.Write"],
      "rank": 50
    },
    {
      "roleId": "...",
      "name": "Administrator",
      "description": "Full system access",
      "permissions": ["System.Read", "System.Write", "System.Admin"],
      "rank": 999
    }
  ]
}
```

### Predefined Roles
- **Reader** (Rank: 1): `System.Read` permission
- **Writer** (Rank: 50): `System.Read`, `System.Write` permissions
- **Administrator** (Rank: 999): `System.Read`, `System.Write`, `System.Admin` permissions

### Database Schema
Roles and permissions are stored in the `weatherdb` database:
- **Users** table: User identifiers and email
- **Roles** table: Role definitions with JSONB permissions column
- **UserRoles** table: Many-to-many mapping between users and roles

See [Authorization Quickstart Guide](specs/001-user-roles-permissions/quickstart.md) for implementation details and integration examples.

## Azure KeyVault Integration

The application supports Azure Key Vault for secrets management:

### Local Development
Set the KeyVault URL using .NET user secrets:
```bash
dotnet user-secrets set "KeyVault:Url" "<your-keyvault-url>"
```

### Azure Production
The application uses `DefaultAzureCredential` with managed identity to access Key Vault automatically.

## Database Migration Scripts

Manual SQL migration scripts are stored in `backend/infra/migrations/`:
- `001_initial_schema.sql`: Creates Forecast and Product tables with idempotent execution
- `002_add_indexes.sql`: Adds performance indexes

All scripts use PostgreSQL advisory locks to ensure idempotent execution in concurrent deployment scenarios.

### Executing Migrations

**Locally:**
```bash
psql -h localhost -U mfeportal -d <database-name> -f backend/infra/migrations/<migration-file>.sql
```

**On Azure:**
```bash
az postgres flexible-server execute -g <resource-group> -s <server-name> -u pgadmin -p <password> -d <database-name> < backend/infra/migrations/<migration-file>.sql
```

## Cleanup

Stop and remove local PostgreSQL:
```bash
cd backend
docker-compose down -v
```

The `-v` flag removes the PostgreSQL volume, clearing all local data. Omit `-v` to preserve data between restarts.
