# Implementation Plan: User Roles and Permissions Component

**Branch**: `001-user-roles-permissions` | **Date**: January 26, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-user-roles-permissions/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement a backend authorization component that provides authenticated users with their roles and permissions. The system supports three roles (Reader, Writer, Administrator) where each role can have multiple permissions stored as a list. Permissions follow a "Resource.Action" naming pattern (e.g., "System.Read", "System.Write"). Each role has a Rank property (Reader=1, Writer=50, Administrator=999) for hierarchy; when a user has multiple roles, the highest rank determines the primary role. Users are domain entities with UserId (Guid) and Email properties. Users can retrieve their permissions, check specific permission grants, and administrators can view all available roles. Role definitions are maintained in a Permissions.cs file. The implementation will follow Clean Architecture principles with Entity Framework Core for persistence (using PostgreSQL JSONB for permissions storage), caching for performance, and OpenAPI documentation.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: ASP.NET Core 9.0, Entity Framework Core 9.0, MediatR 12.4.1, FluentValidation 11.11.0  
**Storage**: PostgreSQL (via Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4), in-memory caching for session-duration permission cache  
**Testing**: xUnit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0, TestContainers 4.3.0, Aspire.Hosting.Testing 9.1.0  
**Target Platform**: Linux containers via Azure Container Apps, local development via .NET Aspire  
**Project Type**: Web API (backend microservice within existing AugmentService)  
**Performance Goals**: <200ms permission retrieval, <50ms permission check, 100 concurrent requests without degradation  
**Constraints**: Must follow Clean Architecture layers, HTTPS-only, zero secrets in git, 90%+ cache hit rate for permissions  
**Scale/Scope**: Single microservice integration, 3 roles, 3 permissions, extensible data model for future growth

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Check (Before Phase 0)

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Clean Architecture** | ✅ PASS | Feature will be implemented across all four layers: Core (entities, interfaces), Application (services, DTOs), Infrastructure (repositories, EF DbContext), API (controllers/endpoints) |
| **II. Micro-Frontend Architecture** | ✅ N/A | Backend-only feature, no frontend impact |
| **III. Security-First Development** | ✅ PASS | HTTPS-only enforced, no secrets in code, authentication required for all endpoints, admin check for role listing |
| **IV. Testing & Quality Assurance** | ✅ PASS | Unit tests for Core/Application, integration tests for Infrastructure/repositories, health checks already exist |
| **V. Documentation & API Standards** | ✅ PASS | OpenAPI/Swagger with Scalar UI already configured, will add XML comments for new endpoints |
| **VI. Cloud-Native Architecture** | ✅ PASS | Uses existing Aspire orchestration, PostgreSQL Flexible Server, no new infrastructure required |
| **VII. Observability & Monitoring** | ✅ PASS | ILogger structured logging, OpenTelemetry via Aspire, will add logging for permission checks |

**Overall**: ✅ **ALL GATES PASS** - No violations, all principles satisfied

---

### Post-Design Check (After Phase 1)

| Principle | Status | Verification |
|-----------|--------|--------------|
| **I. Clean Architecture** | ✅ PASS | ✓ Core layer: Role, Permission, UserRole entities with zero dependencies<br>✓ Application layer: AuthorizationService with business logic<br>✓ Infrastructure layer: EF repositories implementing Core interfaces<br>✓ API layer: Controllers orchestrating Application services<br>✓ Dependencies flow inward: API→Application→Core←Infrastructure |
| **II. Micro-Frontend Architecture** | ✅ N/A | Backend-only feature, no frontend components |
| **III. Security-First Development** | ✅ PASS | ✓ All endpoints require JWT Bearer authentication ([Authorize])<br>✓ Admin permission check for /roles endpoint (403 Forbidden)<br>✓ No secrets in code - JWT configured via appsettings/Key Vault<br>✓ HTTPS enforced (localhost:7001, Azure Container Apps)<br>✓ Graceful handling of expired sessions (401 Unauthorized) |
| **IV. Testing & Quality Assurance** | ✅ PASS | ✓ Unit tests planned: AuthorizationService, permission aggregation logic<br>✓ Integration tests planned: RoleRepository, UserRoleRepository with TestContainers<br>✓ Health checks: existing /health endpoint covers database connectivity<br>✓ 80%+ coverage target achievable with layered testing approach |
| **V. Documentation & API Standards** | ✅ PASS | ✓ OpenAPI specification: contracts/authorization-api.yaml (complete)<br>✓ XML comments planned for all controllers/endpoints<br>✓ Scalar UI pre-configured for interactive documentation<br>✓ Quickstart guide created<br>✓ Data model documented<br>✓ README integration pending |
| **VI. Cloud-Native Architecture** | ✅ PASS | ✓ Uses Aspire for orchestration (no changes needed)<br>✓ PostgreSQL via existing Flexible Server integration<br>✓ Stateless service design (session caching via IMemoryCache)<br>✓ No new Bicep changes required<br>✓ Containerized via existing Container Apps deployment |
| **VII. Observability & Monitoring** | ✅ PASS | ✓ ILogger structured logging planned for all permission checks<br>✓ OpenTelemetry via Aspire (automatic)<br>✓ Health endpoint integration (EF Core health check)<br>✓ Metrics: cache hit rate, permission check latency<br>✓ Error handling: catch at API boundary, log with context |

**Overall**: ✅ **ALL GATES PASS** - Design compliant with all constitution principles

**Complexity Justification**: NOT REQUIRED - No violations detected

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/AugmentService/
├── AugmentService.Core/              # Domain Layer
│   ├── Entities/
│   │   ├── BaseEntity.cs            # Existing
│   │   ├── User.cs                  # NEW - User entity (UserId, Email)
│   │   ├── Role.cs                  # NEW - Role entity (with Permissions list and Rank)
│   │   └── UserRole.cs              # NEW - User-Role assignment
│   ├── Interfaces/
│   │   ├── IUserRepository.cs       # NEW
│   │   ├── IRoleRepository.cs       # NEW
│   │   └── IUserRoleRepository.cs   # NEW
│   └── Permissions.cs               # NEW - Static role definitions and permission constants
│
├── AugmentService.Application/       # Application Layer
│   ├── Services/
│   │   └── AuthorizationService.cs  # NEW - Core business logic
│   ├── Interfaces/
│   │   └── IAuthorizationService.cs # NEW
│   └── DTOs/
│       ├── UserPermissionsDto.cs    # NEW
│       ├── RoleDto.cs               # NEW
│       └── PermissionCheckDto.cs    # NEW
│
├── AugmentService.Infrastructure/    # Infrastructure Layer
│   ├── Data/
│   │   └── AuthorizationDbContext.cs # NEW or extend existing
│   ├── Repositories/
│   │   ├── UserRepository.cs        # NEW
│   │   ├── RoleRepository.cs        # NEW
│   │   └── UserRoleRepository.cs    # NEW
│   └── Caching/
│       └── PermissionCacheService.cs # NEW
│
└── AugmentService.Api/               # API Layer
    ├── Controllers/
    │   └── AuthorizationController.cs # NEW
    ├── Endpoints/
    │   └── AuthorizationEndpoints.cs  # NEW (if using Minimal APIs)
    └── Models/
        ├── GetPermissionsRequest.cs   # NEW
        └── CheckPermissionRequest.cs  # NEW

tests/AugmentService/
├── Core.Tests/
│   └── Entities/
│       └── RoleTests.cs               # NEW
├── Application.Tests/
│   └── Services/
│       └── AuthorizationServiceTests.cs # NEW
└── Infrastructure.Tests/
    └── Repositories/
        └── RoleRepositoryTests.cs     # NEW
```

**Structure Decision**: Using existing Clean Architecture structure within AugmentService. The feature integrates into the existing backend/AugmentService microservice following the established four-layer pattern (Core → Application → Infrastructure → API). No new projects needed; all code added to existing projects.

## Complexity Tracking

*No complexity violations detected - this section intentionally left empty.*

All constitution principles are satisfied. The implementation follows established patterns within the existing Clean Architecture structure.

---

## Planning Phase Summary

### Artifacts Generated

✅ **Phase 0: Research** (completed)
- [research.md](research.md) - Technical decisions for caching, EF Core patterns, authentication, testing

✅ **Phase 1: Design & Contracts** (completed)
- [data-model.md](data-model.md) - Entity definitions, relationships, database schema
- [contracts/authorization-api.yaml](contracts/authorization-api.yaml) - OpenAPI specification with all endpoints
- [quickstart.md](quickstart.md) - Getting started guide for developers
- Agent context updated: GitHub Copilot now aware of technologies used

### Key Decisions Made

| Area | Decision | Document Reference |
|------|----------|-------------------|
| Caching Strategy | IMemoryCache with session-scoped keys | [research.md#1](research.md) |
| Data Model | Explicit UserRole join entity, Permissions as JSONB array | [research.md#2](research.md) |
| Authentication | JWT Bearer with claims-based auth | [research.md#3](research.md) |
| Repository Pattern | Repository per aggregate root | [research.md#4](research.md) |
| Database Seeding | EF Core HasData in OnModelCreating, roles defined in Permissions.cs | [research.md#5](research.md) |
| API Documentation | XML comments + Scalar UI | [research.md#6](research.md) |
| Testing Strategy | Layered: Unit + Integration with TestContainers | [research.md#7](research.md) |
| Permissions Storage | PostgreSQL JSONB for efficient querying of permission arrays | [data-model.md](data-model.md) |

### Architecture Compliance

✅ Clean Architecture - 4 layers defined  
✅ Security-First - JWT auth, HTTPS, no secrets  
✅ Testing & QA - Unit + Integration strategy  
✅ Documentation - OpenAPI spec, quickstart, data model  
✅ Cloud-Native - Aspire, PostgreSQL, containerized  
✅ Observability - Structured logging, health checks  

### What's Next

**Ready for**: `/speckit.tasks` - Generate implementation tasks

**Implementation Readiness**:
- ✅ All technical unknowns resolved
- ✅ Data model defined with validation rules
- ✅ API contracts specified (OpenAPI)
- ✅ Repository and service patterns documented
- ✅ Testing strategy defined
- ✅ Performance targets established (<200ms, 90%+ cache hit)
- ✅ Security requirements clear (JWT, HTTPS, admin checks)

**Estimated Implementation Effort**: 
- Core entities: 2-3 hours
- Application services: 3-4 hours  
- Infrastructure repositories: 3-4 hours
- API controllers: 2-3 hours
- Tests: 4-5 hours
- Database migration: 1 hour
- **Total**: 15-20 hours for complete MVP

---

**Planning Phase Complete**: All design artifacts ready. Proceed with `/speckit.tasks` to generate actionable implementation tasks.
