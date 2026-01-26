# Tasks: User Roles and Permissions Component

**Feature Branch**: `001-user-roles-permissions`  
**Prerequisites**: plan.md, spec.md (3 user stories), research.md (7 decisions), data-model.md (4 entities), contracts/authorization-api.yaml (3 endpoints)

**Tech Stack**: C# / .NET 10.0, ASP.NET Core 9.0, EF Core 9.0, PostgreSQL with JSONB

**Tests**: Included based on Testing & Quality Assurance constitution principle and research.md testing strategy

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize project structure within existing AugmentService

- [X] T001 Verify Clean Architecture project structure exists: Core, Application, Infrastructure, API layers in backend/AugmentService/
- [X] T002 Create feature branch `001-user-roles-permissions` from main branch
- [X] T003 [P] Create Permissions.cs static class in backend/AugmentService/AugmentService.Core/Permissions.cs with System.Read/Write/Admin constants and role definitions (Reader rank=1, Writer rank=50, Administrator rank=999)

**Checkpoint**: Project structure validated, feature branch ready

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain entities and database infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story implementation can begin until this phase is complete

- [X] T004 [P] Create User.cs entity in backend/AugmentService/AugmentService.Core/Entities/User.cs with UserId (Guid PK), Email (string, unique), inheriting from BaseEntity, navigation to UserRoles collection
- [X] T005 [P] Create Role.cs entity in backend/AugmentService/AugmentService.Core/Entities/Role.cs with Id, Name (unique), Description, Permissions (List<string>), Rank (int 1-999), IsActive, navigation to UserRoles collection
- [X] T006 [P] Create UserRole.cs join entity in backend/AugmentService/AugmentService.Core/Entities/UserRole.cs with UserId (FK), RoleId (FK), navigation to Role
- [X] T007 [P] Create IUserRepository interface in backend/AugmentService/AugmentService.Core/Interfaces/IUserRepository.cs with GetByIdAsync, AddAsync methods
- [X] T008 [P] Create IRoleRepository interface in backend/AugmentService/AugmentService.Core/Interfaces/IRoleRepository.cs with GetByIdAsync, GetByNameAsync, GetAllAsync methods
- [X] T009 [P] Create IUserRoleRepository interface in backend/AugmentService/AugmentService.Core/Interfaces/IUserRoleRepository.cs with GetUserRolesAsync, GetUserPermissionsAsync, HasPermissionAsync methods
- [X] T010 Configure DbContext in backend/AugmentService/AugmentService.Infrastructure/Data/UserDbContext.cs (or extend existing DbContext) with Users, Roles, UserRoles DbSets, Fluent API for Role.Permissions JSONB column mapping, relationships, unique constraints, GIN index on Permissions
- [X] T011 Create EF Core migration named AddRolesAndPermissions: dotnet ef migrations add AddRolesAndPermissions --project backend/AugmentService/AugmentService.Infrastructure
- [X] T012 Add HasData seed configuration in OnModelCreating using Permissions.cs role definitions (Reader, Writer, Administrator with fixed GUIDs and rank values)
- [X] T013 Apply database migration: dotnet ef database update (creates Users, Roles, UserRoles tables with seed data)
- [X] T014 [P] Implement UserRepository in backend/AugmentService/AugmentService.Infrastructure/Repositories/UserRepository.cs with CRUD operations
- [X] T015 [P] Implement RoleRepository in backend/AugmentService/AugmentService.Infrastructure/Repositories/RoleRepository.cs with GetByIdAsync, GetByNameAsync, GetAllAsync using EF Core
- [X] T016 [P] Implement UserRoleRepository in backend/AugmentService/AugmentService.Infrastructure/Repositories/UserRoleRepository.cs with GetUserRolesAsync (Include Role, filter IsActive), GetUserPermissionsAsync (SelectMany permissions, Distinct), HasPermissionAsync (EF Core Contains for JSONB)
- [X] T017 Register repositories in DI container in backend/AugmentService/AugmentService.Infrastructure/DependencyInjection.cs (or Program.cs): AddScoped for IUserRepository, IRoleRepository, IUserRoleRepository

**Checkpoint**: Foundation complete - database schema created, repositories implemented, all user stories can now proceed

---

## Phase 3: User Story 1 - Retrieve My Roles and Permissions (Priority: P1) 🎯 MVP

**Goal**: Authenticated users can retrieve their complete permission set in under 200ms

**Independent Test**: Authenticate user with specific roles, call GET /my-permissions, verify returned roles and aggregated permissions match database assignments

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T018 [P] [US1] Create UserPermissionServiceTests.cs in backend/tests/AugmentService/Application.Tests/Services/UserPermissionServiceTests.cs with unit test for GetUserPermissionsAsync with single role returns correct permissions list
- [X] T019 [P] [US1] Add unit test in UserPermissionServiceTests.cs for GetUserPermissionsAsync with multiple roles (Reader + Writer) returns distinct union of permissions ["System.Read", "System.Write"]
- [X] T020 [P] [US1] Add unit test in UserPermissionServiceTests.cs for GetUserPermissionsAsync with no assigned roles returns empty permissions array
- [X] T021 [P] [US1] Create RoleRepositoryTests.cs in backend/tests/AugmentService/Infrastructure.Tests/Repositories/RoleRepositoryTests.cs with integration test using TestContainers PostgreSQL to verify GetAllAsync returns seeded roles
- [X] T022 [P] [US1] Add integration test in RoleRepositoryTests.cs for GetByNameAsync returns correct role with JSONB permissions array
- [X] T023 [P] [US1] Create UserRoleRepositoryTests.cs in backend/tests/AugmentService/Infrastructure.Tests/Repositories/UserRoleRepositoryTests.cs with integration test for GetUserPermissionsAsync aggregating permissions from multiple roles using SelectMany

### Implementation for User Story 1

- [X] T024 [P] [US1] Create UserPermissionsDto.cs in backend/AugmentService/AugmentService.Application/DTOs/UserPermissionsDto.cs with UserId, Roles (List<RoleDto>), Permissions (List<string>) properties
- [X] T025 [P] [US1] Create RoleDto.cs in backend/AugmentService/AugmentService.Application/DTOs/RoleDto.cs with RoleId, Name, Description, Permissions (List<string>), Rank properties matching OpenAPI spec
- [X] T026 [P] [US1] Create IUserPermissionService interface (renamed from IUserPermissionService to avoid ASP.NET Core conflict) in backend/AugmentService/AugmentService.Application/Interfaces/IUserPermissionService.cs with GetUserPermissionsAsync method signature
- [X] T027 [US1] Implement UserPermissionService.cs in backend/AugmentService/AugmentService.Application/Services/UserPermissionService.cs with GetUserPermissionsAsync method: retrieve user roles via IUserRoleRepository, aggregate permissions using SelectMany + Distinct, map to UserPermissionsDto with primary role (highest rank) first
- [X] T028 [US1] Integrated IMemoryCache-based caching into UserPermissionService with session-scoped cache keys (permissions:{userId}), 8hr absolute + 30min sliding expiration
- [X] T029 [US1] Cache integration complete: check cache first, on miss fetch from repository and populate cache
- [X] T030 [US1] Create UserController.cs in backend/AugmentService/AugmentService.Api/Controllers/UserController.cs with [Authorize] attribute and GetMyPermissions endpoint
- [X] T031 [US1] Implement GetMyPermissions() GET endpoint in UserController: extract userId from JWT claims (ClaimTypes.NameIdentifier), call IUserPermissionService.GetUserPermissionsAsync, return 200 OK with UserPermissionsDto, handle 401 if no userId claim
- [X] T032 [US1] Add XML documentation comments to GetMyPermissions endpoint for OpenAPI generation: summary, returns tag, response codes 200/401, example response
- [X] T033 [US1] Register IUserPermissionService in DI container in backend/AugmentService/AugmentService.Application/DependencyInjection.cs: AddScoped<IUserPermissionService, UserPermissionService> + AddMemoryCache()
- [X] T034 [US1] Add structured logging in UserPermissionService.GetUserPermissionsAsync: log userId, cache hit/miss, permissions count, elapsed time using ILogger

**Checkpoint**: User Story 1 complete - users can retrieve their permissions via GET /my-permissions with caching

---

## Phase 4: User Story 2 - Check Specific Permission (Priority: P2)

**Goal**: Provide convenient helper endpoint to check if user has specific permission in under 50ms

**Independent Test**: Authenticate users with different role combinations, call POST /check-permission with various permission names, verify correct true/false responses

### Tests for User Story 2

- [ ] T035 [P] [US2] Add unit test in UserPermissionServiceTests.cs for HasPermissionAsync returns true when user has permission "System.Write"
- [ ] T036 [P] [US2] Add unit test in UserPermissionServiceTests.cs for HasPermissionAsync returns false when user lacks permission "System.Admin"
- [ ] T037 [P] [US2] Add unit test in UserPermissionServiceTests.cs for HasPermissionAsync returns false for non-existent permission (not an error)
- [ ] T038 [P] [US2] Add integration test in UserRoleRepositoryTests.cs for HasPermissionAsync using EF Core JSONB Contains operator verifies correct boolean result

### Implementation for User Story 2

- [ ] T039 [P] [US2] Create CheckPermissionRequest.cs in backend/AugmentService/AugmentService.Application/DTOs/CheckPermissionRequest.cs with Permission (string, required) property and FluentValidation rules (not null/empty, matches pattern "^[A-Za-z]+\.[A-Za-z]+$")
- [ ] T040 [P] [US2] Create CheckPermissionResponse.cs in backend/AugmentService/AugmentService.Application/DTOs/CheckPermissionResponse.cs with Permission (string) and HasPermission (bool) properties
- [ ] T041 [US2] Add HasPermissionAsync method to IUserPermissionService interface with userId and permission name parameters
- [ ] T042 [US2] Implement UserPermissionService.HasPermissionAsync: retrieve cached permissions via GetUserPermissionsAsync (reuse US1 caching), check if permission exists in list, return boolean
- [ ] T043 [US2] Implement CheckPermission() POST endpoint in UserController: extract userId from JWT claims, validate request body, call IUserPermissionService.HasPermissionAsync, return 200 OK with CheckPermissionResponse
- [ ] T044 [US2] Add XML documentation comments to CheckPermission endpoint with summary, param description, response codes 200/400/401
- [ ] T045 [US2] Add FluentValidation validator registration for CheckPermissionRequest in DI container

**Checkpoint**: User Story 2 complete - users can check specific permissions via POST /check-permission leveraging US1 caching

---

## Phase 5: User Story 3 - View Available Roles (Priority: P3)

**Goal**: Administrators can view all system roles with their permissions for role management

**Independent Test**: Authenticate as admin, call GET /roles, verify all 3 roles returned; authenticate as non-admin, call GET /roles, verify 403 Forbidden

### Tests for User Story 3

- [ ] T046 [P] [US3] Add unit test in UserPermissionServiceTests.cs for GetAllRolesAsync returns all active roles ordered by name
- [ ] T047 [P] [US3] Add integration test for UserController.GetAllRoles: mock user with Admin permission, verify 200 OK with all roles
- [ ] T048 [P] [US3] Add integration test for UserController.GetAllRoles: mock user without Admin permission, verify 403 Forbidden response

### Implementation for User Story 3

- [ ] T049 [P] [US3] Create RolesListResponse.cs in backend/AugmentService/AugmentService.Application/DTOs/RolesListResponse.cs with Roles (List<RoleDto>) property
- [ ] T050 [US3] Add GetAllRolesAsync method to IUserPermissionService interface returning Task<IEnumerable<RoleDto>>
- [ ] T051 [US3] Implement UserPermissionService.GetAllRolesAsync: call IRoleRepository.GetAllAsync, filter IsActive = true, order by Name, map to List<RoleDto>
- [ ] T052 [US3] Implement GetAllRoles() GET endpoint in UserController with [Authorize] attribute
- [ ] T053 [US3] Add admin permission check in GetAllRoles endpoint: call HasPermissionAsync for "System.Admin", return 403 Forbidden if false, otherwise return 200 OK with RolesListResponse
- [ ] T054 [US3] Add XML documentation comments to GetAllRoles endpoint with summary, admin requirement note, response codes 200/401/403

**Checkpoint**: User Story 3 complete - admins can list all roles via GET /roles with permission enforcement

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements affecting multiple user stories

- [X] T055 [P] Add GlobalExceptionHandler or exception middleware in backend/AugmentService/AugmentService.Api/Middleware/ExceptionHandlerMiddleware.cs to catch unhandled exceptions, log with structured logging, return consistent error JSON format per OpenAPI ErrorResponse schema
- [X] T056 [P] Add validation for Role.Permissions pattern "^[A-Za-z]+\.[A-Za-z]+$" in Role entity using FluentValidation or data annotations
- [X] T057 [P] Add validation for Role.Rank range (1-999) using data annotations with RangeAttribute in Role.cs
- [X] T058 [P] Configure HTTPS-only enforcement in backend/AugmentService/AugmentService.Api/Program.cs using UseHttpsRedirection middleware
- [ ] T059 Verify OpenAPI spec generation: start application, navigate to https://localhost:7001/scalar/v1, confirm all 3 endpoints visible under Authorization tag with XML comment documentation
- [ ] T060 [P] Add health check verification: call GET /health endpoint, confirm database connectivity check includes Roles table
- [ ] T061 Run quickstart.md validation: execute all steps in quickstart guide as new developer, verify no errors, update guide if steps need clarification
- [ ] T062 [P] Performance validation: use ApacheBench or k6 to test GET /my-permissions endpoint with 100 concurrent requests, verify <200ms average latency (first call) and <5ms (cached calls)
- [ ] T063 [P] Cache hit rate validation: enable logging for cache hits/misses in PermissionCacheService, simulate 100 requests, verify >90% cache hit rate
- [ ] T064 [P] Security audit: verify no JWT secrets in code, all endpoints require [Authorize], admin check enforced on /roles, HTTPS enforced, 401/403 returned correctly
- [X] T065 Update SOLUTION_SUMMARY.md or README.md in repository root with new Authorization API section documenting the 3 endpoints and quickstart reference
- [X] T066 Update .github/agents/copilot-instructions.md with completed feature: User Roles and Permissions API with JWT Bearer auth, PostgreSQL JSONB permissions, session caching

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Foundational phase (T004-T017) - Can start after T017 complete
- **User Story 2 (Phase 4)**: Depends on Foundational phase (T004-T017) - Can start after T017 complete, **reuses US1 caching**
- **User Story 3 (Phase 5)**: Depends on Foundational phase (T004-T017) - Can start after T017 complete, **reuses US1 services**
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Independent after Foundational phase - No dependencies on other stories
- **User Story 2 (P2)**: Builds on US1's UserPermissionService and PermissionCacheService - Should implement US1 first for efficiency, but technically independent
- **User Story 3 (P3)**: Reuses US1's UserPermissionService.HasPermissionAsync for admin check - Should implement US1 first

### Within Each User Story

1. **Tests** (if included) MUST be written and FAIL before implementation
2. **DTOs** before services
3. **Repository interfaces** before implementations
4. **Services** before controllers
5. **Core logic** before API endpoints
6. **Story complete** before moving to next priority

### Parallel Opportunities

**Phase 1 (Setup)**:
- T001-T003 are sequential (verify, then branch, then create file)

**Phase 2 (Foundational)**:
- T004-T009 marked [P]: All Core layer entities and interfaces can be created in parallel
- T010-T013: Sequential (DbContext config → migration create → seed data → migration apply)
- T014-T016 marked [P]: All repository implementations can be created in parallel after T010-T013
- T017: Must be last in phase

**Phase 3 (User Story 1 - Tests)**:
- T018-T023 marked [P]: All tests can be written in parallel

**Phase 3 (User Story 1 - Implementation)**:
- T024-T025 marked [P]: Both DTOs can be created in parallel
- T026-T029: Sequential (interface → service → cache service → integration)
- T030-T034: Sequential (controller → endpoint → XML docs → DI registration → logging)

**Phase 4 (User Story 2 - Tests)**:
- T035-T038 marked [P]: All tests can be written in parallel

**Phase 4 (User Story 2 - Implementation)**:
- T039-T040 marked [P]: Both DTOs can be created in parallel
- T041-T045: Sequential (interface → implementation → endpoint → docs → validation)

**Phase 5 (User Story 3 - Tests)**:
- T046-T048 marked [P]: All tests can be written in parallel

**Phase 5 (User Story 3 - Implementation)**:
- T049 [P]: DTO creation
- T050-T054: Sequential (interface → implementation → endpoint → admin check → docs)

**Phase 6 (Polish)**:
- T055-T058 marked [P]: Exception handler, validation rules, HTTPS config can be done in parallel
- T059-T064: Can be executed in parallel (all verification tasks)
- T065-T066 marked [P]: Documentation updates can be done in parallel

---

## Parallel Example: User Story 1 Tests

```bash
# Launch all test file creation for User Story 1 together:
Task T018: "Create UserPermissionServiceTests.cs with single role test"
Task T019: "Add multiple roles test to UserPermissionServiceTests.cs"
Task T020: "Add no roles test to UserPermissionServiceTests.cs"
Task T021: "Create RoleRepositoryTests.cs with GetAllAsync test"
Task T022: "Add GetByNameAsync test to RoleRepositoryTests.cs"
Task T023: "Create UserRoleRepositoryTests.cs with GetUserPermissionsAsync test"
```

---

## Parallel Example: User Story 1 DTOs

```bash
# Launch both DTO file creation for User Story 1 together:
Task T024: "Create UserPermissionsDto.cs in Application/DTOs/"
Task T025: "Create RoleDto.cs in Application/DTOs/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T017) - **CRITICAL BLOCKER**
3. Complete Phase 3: User Story 1 (T018-T034)
4. **STOP and VALIDATE**: 
   - Run all US1 tests
   - Start application, authenticate, call GET /my-permissions
   - Verify response matches expected format
   - Test caching: call endpoint twice, verify second call <5ms
5. Deploy/demo MVP if ready

### Incremental Delivery

1. **Foundation** (T001-T017) → Database ready, repositories implemented
2. **Add User Story 1** (T018-T034) → Test independently → Deploy/Demo (MVP! 🎯)
   - Users can retrieve their permissions
   - Caching works, <200ms first call, <5ms cached
3. **Add User Story 2** (T035-T045) → Test independently → Deploy/Demo
   - Users can check specific permissions
   - Leverages US1 caching infrastructure
4. **Add User Story 3** (T046-T054) → Test independently → Deploy/Demo
   - Admins can view all roles
   - Admin check enforced with 403 for non-admins
5. **Polish** (T055-T066) → Validate performance/security → Final Deploy
   - Exception handling, validation, HTTPS enforcement
   - Performance metrics verified
   - Documentation updated

### Parallel Team Strategy

With multiple developers:

1. **Team completes Setup + Foundational together** (T001-T017)
2. Once Foundational is done:
   - **Developer A**: User Story 1 (T018-T034)
   - **Developer B**: User Story 2 (T035-T045) - Can start after T034, reuses US1 services
   - **Developer C**: User Story 3 (T046-T054) - Can start after T034, reuses US1 services
3. Stories complete independently, merge sequentially (US1 → US2 → US3)
4. **Team completes Polish together** (T055-T066)

---

## Estimated Effort

**Total Tasks**: 66 tasks

**Phase Breakdown**:
- Phase 1 (Setup): 3 tasks → ~30 minutes
- Phase 2 (Foundational): 14 tasks → ~6-8 hours (includes migration, repositories, DbContext config)
- Phase 3 (User Story 1): 17 tasks → ~6-8 hours (includes 6 tests, 11 implementation tasks)
- Phase 4 (User Story 2): 11 tasks → ~3-4 hours (includes 4 tests, 7 implementation tasks)
- Phase 5 (User Story 3): 9 tasks → ~2-3 hours (includes 3 tests, 6 implementation tasks)
- Phase 6 (Polish): 12 tasks → ~4-5 hours (validation, documentation, security audit)

**Total Estimated Effort**: 22-28 hours for complete feature with tests

**MVP Only (User Story 1)**: ~15-18 hours (Setup + Foundational + US1)

---

## Notes

- All tasks follow strict checklist format: `- [ ] [TaskID] [P?] [Story?] Description with file path`
- [P] tasks use different files or have no dependencies on incomplete tasks
- [Story] labels (US1, US2, US3) enable independent story tracking
- Tests are written FIRST for each user story, verify they FAIL before implementation
- Each user story is independently completable and testable
- Commit after each task or logical group for safe progress tracking
- Stop at any checkpoint to validate story independently before proceeding
- Foundation phase (T004-T017) is critical blocker - all stories depend on it
- User Story 2 and 3 reuse User Story 1 infrastructure (caching, services) for efficiency

---

**Task Breakdown Complete**: 66 actionable tasks ready for `/speckit.implement` command execution
