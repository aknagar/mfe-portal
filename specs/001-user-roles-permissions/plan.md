# Implementation Plan: User Roles and Permissions Component

**Branch**: `001-user-roles-permissions` | **Date**: 2026-01-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-user-roles-permissions/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement a user roles and permissions system that provides authenticated users with their role-based permissions through REST API endpoints. The system authenticates users via Azure AD/Entra ID (OpenID Connect), stores role assignments in PostgreSQL, and caches permissions in-memory for performance. Three predefined roles (Reader, Writer, Administrator) with hierarchical permissions enable granular authorization enforcement across the application.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: ASP.NET Core 9.0, Entity Framework Core 10.0.1, Npgsql 10.0.0, Microsoft.IdentityModel.JsonWebTokens 8.0.0, .NET Aspire 13.1.0  
**Storage**: PostgreSQL (via Npgsql.EntityFrameworkCore.PostgreSQL)  
**Testing**: xUnit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0, Testcontainers.PostgreSql 4.3.0  
**Target Platform**: Azure Container Apps (via .NET Aspire orchestration)  
**Project Type**: Backend microservice within existing Aspire-based distributed application  
**Performance Goals**: <200ms p95 for permission retrieval, <50ms p95 for permission checks, support 100 concurrent requests  
**Constraints**: Auto-scaling 1-10 replicas, in-memory cache (no distributed cache), session-based permission consistency  
**Scale/Scope**: Multi-tenant, user-scoped authorization, 3 predefined roles, extensible permission model

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Clean Architecture** | ✅ PASS | Backend service will follow existing pattern: Core (entities, interfaces) → Application (services, DTOs) → Infrastructure (EF repositories) → API (UserController). No violations. |
| **III. Security-First** | ✅ PASS | Azure AD authentication (OpenID Connect) enforced via ASP.NET Core middleware. No secrets in code (Azure Key Vault for production). HTTPS mandatory. Auto-provisioning prevents unauthorized access. |
| **IV. Testing & Quality** | ✅ PASS | Unit tests (Core/Application layers), integration tests (Infrastructure with Testcontainers), health endpoints required. Test coverage target 80%+. |
| **V. Documentation & API** | ✅ PASS | OpenAPI/Swagger via Scalar.AspNetCore (already in use). UserController endpoints documented with XML comments. README updates required for role management. |
| **VI. Cloud-Native** | ✅ PASS | Integrates with existing .NET Aspire AppHost. PostgreSQL via Aspire orchestration. Supports `azd up` deployment. IMemoryCache suitable for 1-10 replica scale (stateless per request). |
| **VII. Observability** | ✅ PASS | ILogger structured logging. OpenTelemetry via Aspire ServiceDefaults. Health endpoints `/health` and `/alive` required. Distributed tracing for permission lookups. |

**Constitution Compliance**: ✅ **APPROVED** - No violations. Feature aligns with all applicable principles.

## Project Structure

### Documentation (this feature)

```text
specs/001-user-roles-permissions/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── user-api.yaml    # OpenAPI spec for UserController endpoints
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/AugmentService/
├── AugmentService.Core/                    # EXISTING - Domain layer
│   ├── Entities/
│   │   ├── User.cs                        # ✅ EXISTS - needs validation
│   │   ├── Role.cs                        # ✅ EXISTS - needs validation
│   │   └── UserRole.cs                    # ✅ EXISTS - join table
│   ├── Interfaces/
│   │   ├── IUserRepository.cs             # NEW - user data access
│   │   ├── IRoleRepository.cs             # NEW - role data access
│   │   └── IPermissionService.cs          # NEW - permission logic abstraction
│   ├── Permissions.cs                     # ✅ EXISTS - permission constants & role definitions
│   └── Attributes/
│       └── PermissionPatternAttribute.cs  # NEW - validation attribute for "Resource.Action" pattern
│
├── AugmentService.Application/            # EXISTING - Business logic layer
│   ├── DTOs/
│   │   ├── UserPermissionsDto.cs         # NEW - response for GET /me/permissions
│   │   ├── PermissionCheckDto.cs         # NEW - request/response for permission check
│   │   └── RoleDto.cs                    # NEW - response for GET /roles
│   ├── Services/
│   │   ├── PermissionService.cs          # NEW - permission aggregation, caching, business rules
│   │   └── UserProvisioningService.cs    # NEW - auto-provisioning logic
│   └── Interfaces/
│       ├── IPermissionService.cs         # MOVE from Core (Application depends on Core)
│       └── IUserProvisioningService.cs   # NEW - provisioning abstraction
│
├── AugmentService.Infrastructure/         # EXISTING - Data access layer
│   ├── Data/
│   │   ├── AppDbContext.cs               # MODIFY - add User, Role, UserRole DbSets
│   │   └── Configurations/
│   │       ├── UserConfiguration.cs      # NEW - EF fluent config for User
│   │       ├── RoleConfiguration.cs      # NEW - EF fluent config for Role
│   │       └── UserRoleConfiguration.cs  # NEW - EF fluent config for UserRole
│   ├── Repositories/
│   │   ├── UserRepository.cs             # NEW - implements IUserRepository
│   │   └── RoleRepository.cs             # NEW - implements IRoleRepository
│   └── Seeders/
│       └── RoleSeeder.cs                 # NEW - seed predefined roles from Permissions.Roles
│
└── AugmentService.Api/                    # EXISTING - API layer
    ├── Controllers/
    │   └── UserController.cs             # NEW - /me/permissions, /me/permissions/{name}, /roles
    ├── Middleware/
    │   └── UserProvisioningMiddleware.cs # NEW - auto-provision on first request
    ├── Program.cs                        # MODIFY - register Azure AD, services, middleware
    └── appsettings.json                  # MODIFY - add AzureAd section, cache timeout config

tests/AugmentService/
├── AugmentService.Core.Tests/            # NEW test project
│   └── Entities/
│       └── RoleTests.cs                  # NEW - test role rank logic, permission validation
│
├── AugmentService.Application.Tests/     # NEW test project  
│   └── Services/
│       ├── PermissionServiceTests.cs     # NEW - test permission aggregation, caching
│       └── UserProvisioningServiceTests.cs # NEW - test auto-provision logic
│
└── AugmentService.Integration.Tests/     # EXISTING (modify)
    ├── UserControllerTests.cs            # NEW - test endpoints with TestServer + Testcontainers
    └── UserRepositoryTests.cs            # NEW - test repository with Testcontainers PostgreSQL
```

**Structure Decision**: Following existing Clean Architecture pattern in AugmentService. Core entities already exist (User, Role, UserRole, Permissions.cs). Implementation focuses on Application services (permission logic, caching), Infrastructure repositories (EF Core), and API layer (UserController, Azure AD middleware). Tests follow existing xUnit + Testcontainers pattern.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No violations detected. All architecture and security requirements align with constitution principles.*

---

## Post-Design Constitution Re-Check

*Re-evaluation after Phase 1 design artifacts completed.*

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| **I. Clean Architecture** | ✅ PASS | Design confirms layering: Core entities verified (User, Role, UserRole exist), Application services planned (PermissionService, UserProvisioningService), Infrastructure repositories defined (UserRepository, RoleRepository), API layer constrained to UserController. Dependencies flow inward. |
| **III. Security-First** | ✅ PASS | Microsoft.Identity.Web integration specified, Azure AD configuration defined, auto-provisioning middleware ensures no authentication bypass, HTTPS enforced via Aspire, sensitive data (emails) not logged in structured logs. |
| **IV. Testing & Quality** | ✅ PASS | Test project structure defined (Core.Tests, Application.Tests, Integration.Tests), Testcontainers PostgreSQL for integration tests, unit test coverage planned for permission aggregation logic. |
| **V. Documentation & API** | ✅ PASS | OpenAPI contract completed (contracts/authorization-api.yaml), XML comments planned for controller methods, quickstart.md exists for local setup, data-model.md documents schema. |
| **VI. Cloud-Native** | ✅ PASS | Integrates with existing Aspire AppHost, PostgreSQL via Aspire orchestration confirmed, IMemoryCache appropriate for stateless design (1-10 replicas), no distributed cache dependency added. |
| **VII. Observability** | ✅ PASS | ILogger usage planned in services, structured logging with email context, health checks inherit from ServiceDefaults, OpenTelemetry tracing via Aspire for permission lookups. |

**Post-Design Compliance**: ✅ **APPROVED** - Design artifacts maintain full compliance. No new violations introduced. Ready for Phase 2 (tasks generation).
