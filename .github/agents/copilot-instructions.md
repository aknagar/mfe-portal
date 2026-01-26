# mfe-portal Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-01-26

## Active Technologies
- C# / .NET 10.0 + ASP.NET Core 9.0, Entity Framework Core 10.0.1, Npgsql 10.0.0, Microsoft.IdentityModel.JsonWebTokens 8.0.0, .NET Aspire 13.1.0 (001-user-roles-permissions)
- PostgreSQL (via Npgsql.EntityFrameworkCore.PostgreSQL) (001-user-roles-permissions)

- C# / .NET 10.0 + ASP.NET Core 9.0, Entity Framework Core 10.0, MediatR 12.4.1, FluentValidation 11.11.0 (001-user-roles-permissions)
- PostgreSQL 16 with JSONB for flexible permission storage
- JWT Bearer Authentication for API security
- IMemoryCache for session-scoped permission caching
- xUnit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0, TestContainers 4.3.0 for testing

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for C# / .NET 10.0

## Code Style

C# / .NET 10.0: Follow standard conventions

## Recent Changes
- 001-user-roles-permissions: Added C# / .NET 10.0 + ASP.NET Core 9.0, Entity Framework Core 10.0.1, Npgsql 10.0.0, Microsoft.IdentityModel.JsonWebTokens 8.0.0, .NET Aspire 13.1.0

- 001-user-roles-permissions: Implemented User Roles and Permissions API with:
  - JWT Bearer authentication with ClaimTypes.NameIdentifier extraction
  - PostgreSQL JSONB storage for role permissions with GIN index
  - IMemoryCache session-scoped caching (8hr absolute, 30min sliding expiration)
  - Clean Architecture: Core (entities, interfaces) → Application (services, DTOs) → Infrastructure (repositories, DbContext) → API (controllers)
  - 3 REST endpoints: GET /my-permissions, POST /check-permission, GET /roles (admin-only)
  - 3 predefined roles: Reader (rank 1), Writer (rank 50), Administrator (rank 999)
  - Custom validation attributes: PermissionPattern for "Resource.Action" format
  - Global exception handler for standardized error responses
  - HTTPS enforcement via UseHttpsRedirection middleware
  - OpenAPI documentation with Scalar UI
  - Comprehensive testing: 7 unit tests, 17 integration tests using TestContainers

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
