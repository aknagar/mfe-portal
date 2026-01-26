# Research Document: User Roles and Permissions Component

**Feature**: 001-user-roles-permissions  
**Date**: January 26, 2026  
**Purpose**: Document technical research findings for implementation decisions

---

## Research Areas

### 1. Entity Framework Core Permission Caching Strategies

**Question**: How to implement session-duration permission caching with 90%+ cache hit rate?

**Decision**: Use ASP.NET Core IMemoryCache with session-scoped cache keys

**Rationale**:
- IMemoryCache is built-in, no additional dependencies
- Session-scoped keys (user ID + session ID) ensure proper cache invalidation
- Supports absolute expiration (session timeout) and sliding expiration
- Thread-safe and highly performant
- Cache hit rate >95% achievable with proper key design

**Implementation Approach**:
```csharp
// Cache key pattern: "permissions:{userId}:{sessionId}"
var cacheKey = $"permissions:{userId}:{sessionId}";
if (!_cache.TryGetValue(cacheKey, out List<Permission> permissions))
{
    permissions = await _repository.GetUserPermissionsAsync(userId);
    var cacheOptions = new MemoryCacheEntryOptions()
        .SetAbsoluteExpiration(TimeSpan.FromHours(8)) // Session timeout
        .SetSlidingExpiration(TimeSpan.FromMinutes(30));
    _cache.Set(cacheKey, permissions, cacheOptions);
}
```

**Alternatives Considered**:
- Redis: Overkill for session-scoped caching, adds infrastructure complexity
- Database-level caching: Doesn't reduce query load enough
- Custom in-memory dictionary: Not thread-safe, reinventing the wheel

**References**:
- [Microsoft Docs: Cache in-memory in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- [Memory Cache Best Practices](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache)

---

### 2. Entity Framework Core Many-to-Many Relationships

**Question**: How to model User-Role many-to-many relationship with EF Core 9?

**Decision**: Use explicit join entity (UserRole) with navigation properties

**Rationale**:
- Explicit join entity provides better control for auditing (CreatedDate, etc.)
- Aligns with Clean Architecture principle of explicit domain modeling
- EF Core 9 supports both implicit and explicit approaches; explicit is more extensible
- Allows adding metadata to the relationship (e.g., AssignedBy, AssignedDate)

**Implementation Approach**:
```csharp
// Role.cs - Domain entity
public class Role : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required List<string> Permissions { get; set; } // Multiple permissions like ["System.Read", "System.Write"]
    public required int Rank { get; set; } // Hierarchy: higher rank = higher priority
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

// UserRole.cs - Join entity
public class UserRole : BaseEntity
{
    public required Guid UserId { get; set; }
    public required Guid RoleId { get; set; }
    
    // Navigations
    public Role Role { get; set; } = null!;
}

// EF Configuration - Permissions stored as JSONB
modelBuilder.Entity<Role>()
    .Property(r => r.Permissions)
    .HasColumnType("jsonb");

modelBuilder.Entity<UserRole>()
    .HasOne(ur => ur.Role)
    .WithMany(r => r.UserRoles)
    .HasForeignKey(ur => ur.RoleId);
```

**Alternatives Considered**:
- Implicit many-to-many: Less control, harder to add metadata later
- Separate Permission table with many-to-many: Over-engineered for simple permission strings
- Storing permissions as delimited string: Harder to query, less type-safe than JSONB

**References**:
- [EF Core Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many)
- [Clean Architecture Entity Patterns](https://github.com/jasontaylordev/CleanArchitecture)

---

### 3. ASP.NET Core Authentication & Authorization Best Practices

**Question**: How to authenticate users and enforce authorization on endpoints?

**Decision**: Use JWT Bearer authentication with claims-based authorization

**Rationale**:
- JWT already used in the codebase (Microsoft.IdentityModel.JsonWebTokens in dependencies)
- Claims-based approach maps naturally to permissions
- User ID extracted from JWT claims for permission lookups
- Standard pattern in ASP.NET Core microservices

**Implementation Approach**:
```csharp
// Extract user ID from JWT claims
var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
{
    return Unauthorized();
}

// Require authentication on endpoints
[Authorize] // All endpoints require authentication
[HttpGet("my-permissions")]
public async Task<IActionResult> GetMyPermissions() { ... }

// Admin-only endpoint
[Authorize]
[HttpGet("roles")]
public async Task<IActionResult> GetAllRoles()
{
    // Manual admin check within method
    var hasAdminPermission = await _authService.HasPermissionAsync(userId, "Admin");
    if (!hasAdminPermission)
    {
        return Forbid(); // 403
    }
    ...
}
```

**Alternatives Considered**:
- Custom authorization policies: More complex for simple permission checks
- Role-based [Authorize(Roles = "Admin")]: Less flexible than permission-based checks

**References**:
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt)

---

### 4. Repository Pattern with EF Core

**Question**: What's the optimal repository pattern for this feature?

**Decision**: Use Repository per Aggregate Root with generic base repository

**Rationale**:
- Aligns with existing codebase pattern (InMemoryProxyTargetRepository example found)
- Each entity (Role, Permission, UserRole) gets its own repository interface
- Keeps infrastructure concerns out of Core layer
- Easy to test with in-memory implementations
- Can switch between in-memory (development) and EF Core (production)

**Implementation Approach**:
```csharp
// Core layer interface
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default);
}

// Infrastructure implementation
public class RoleRepository : IRoleRepository
{
    private readonly AuthorizationDbContext _context;
    
    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name, ct);
    }
}

// Registration in DI
services.AddScoped<IRoleRepository, RoleRepository>();
```

**Alternatives Considered**:
- Direct DbContext usage in services: Violates Clean Architecture, couples to EF Core
- Generic repository: Too abstract, loses type-specific query capabilities

**References**:
- Existing codebase: `AugmentService.Infrastructure/Repositories/InMemoryProxyTargetRepository.cs`
- [Repository Pattern with EF Core](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

---

### 5. Database Seeding for Initial Roles and Permissions

**Question**: How to seed the three roles (Reader, Writer, Administrator) on startup?

**Decision**: Use EF Core OnModelCreating HasData for seed data

**Rationale**:
- Declarative seed data in the model configuration
- EF Core migrations automatically include seed data
- Idempotent - won't duplicate on re-run
- Version-controlled as part of migrations
- Simple and standard approach

**Implementation Approach**:
```csharp
// In DbContext OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Seed roles with fixed GUIDs for consistency
    var readerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var writerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    
    modelBuilder.Entity<Role>().HasData(
        new Role 
        { 
            Id = readerRoleId,
            Name = "Reader", 
            Description = "Read-only access",
            Permissions = new List<string> { "System.Read" },
            Rank = 1,
            CreatedDate = DateTime.UtcNow
        },
        new Role 
        { 
            Id = writerRoleId,
            Name = "Writer", 
            Description = "Read and write access",
            Permissions = new List<string> { "System.Read", "System.Write" },
            Rank = 50,
            CreatedDate = DateTime.UtcNow
        },
        new Role 
        { 
            Id = adminRoleId,
            Name = "Administrator", 
            Description = "Full administrative access",
            Permissions = new List<string> { "System.Read", "System.Write", "System.Admin" },
            Rank = 999,
            CreatedDate = DateTime.UtcNow
        }
    );
}
```

**Alternatives Considered**:
- Startup service that inserts data: More complex, requires extra code
- SQL script: Not version-controlled with code, harder to maintain
- Configuration file: Overly complex for static seed data

**References**:
- [EF Core Data Seeding](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)

---

### 6. OpenAPI Documentation Standards

**Question**: How to document the new authorization endpoints in Swagger/OpenAPI?

**Decision**: Use XML comments + Scalar UI (already configured in codebase)

**Rationale**:
- Scalar UI already configured in AugmentService.Api (from dependencies scan)
- XML comments generate rich OpenAPI documentation
- Standard ASP.NET Core approach
- Integrates with existing setup, no changes needed

**Implementation Approach**:
```csharp
/// <summary>
/// Retrieves the current user's roles and permissions
/// </summary>
/// <returns>List of roles and their associated permissions</returns>
/// <response code="200">Successfully retrieved permissions</response>
/// <response code="401">User is not authenticated</response>
[HttpGet("my-permissions")]
[ProducesResponseType(typeof(UserPermissionsDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetMyPermissions() { ... }

// Enable XML comments in .csproj
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

**Alternatives Considered**:
- Manual OpenAPI spec: Error-prone, not synced with code
- Swagger annotations: More verbose than XML comments

**References**:
- Existing: `Scalar.AspNetCore` package in Directory.Packages.props
- [ASP.NET Core OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger)

---

### 7. Testing Strategy for Authorization Logic

**Question**: What testing approach ensures correct permission resolution?

**Decision**: Layered testing - Unit tests for logic, Integration tests for repository/DB

**Rationale**:
- Matches project's existing testing structure (xUnit, NSubstitute, TestContainers found)
- Unit tests verify business logic in isolation (permission aggregation, caching)
- Integration tests verify database queries and relationships
- TestContainers for realistic PostgreSQL integration tests
- Fast feedback loop with unit tests, comprehensive coverage with integration tests

**Implementation Approach**:
```csharp
// Unit test example (Application layer)
[Fact]
public async Task GetUserPermissions_MultipleRoles_AggregatesPermissions()
{
    // Arrange
    var mockRepo = Substitute.For<IUserRoleRepository>();
    mockRepo.GetUserRolesAsync(Arg.Any<Guid>())
        .Returns(new List<Role> 
        { 
            new Role { Permission = "Read" },
            new Role { Permission = "Write" }
        });
    var service = new AuthorizationService(mockRepo, ...);
    
    // Act
    var permissions = await service.GetUserPermissionsAsync(userId);
    
    // Assert
    permissions.Should().Contain("Read");
    permissions.Should().Contain("Write");
    permissions.Should().HaveCount(2);
}

// Integration test example (Infrastructure layer)
[Fact]
public async Task RoleRepository_GetByName_ReturnsCorrectRole()
{
    // Arrange - TestContainers PostgreSQL
    await using var container = new PostgreSqlBuilder().Build();
    await container.StartAsync();
    var dbContext = CreateDbContext(container.GetConnectionString());
    var repository = new RoleRepository(dbContext);
    
    // Act
    var role = await repository.GetByNameAsync("Reader");
    
    // Assert
    role.Should().NotBeNull();
    role.Permission.Should().Be("Read");
}
```

**Alternatives Considered**:
- Only integration tests: Slow feedback loop, harder to debug
- Only unit tests: Doesn't verify actual database behavior

**References**:
- Existing: `tests/` directory structure in codebase
- [TestContainers .NET](https://dotnet.testcontainers.org/)
- [Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

---

## Summary of Decisions

| Area | Decision | Impact |
|------|----------|--------|
| **Caching** | IMemoryCache with session-scoped keys | Built-in, high performance, >90% cache hit rate |
| **Data Model** | Explicit UserRole join entity | Extensible, auditable, aligns with Clean Architecture |
| **Authentication** | JWT Bearer with claims | Standard pattern, matches existing infrastructure |
| **Repository** | Repository per aggregate root | Testable, follows existing codebase pattern |
| **Seeding** | EF Core HasData in OnModelCreating | Declarative, migration-friendly, idempotent |
| **Documentation** | XML comments + Scalar UI | Already configured, standard approach |
| **Testing** | Unit (Application) + Integration (Infrastructure) | Comprehensive coverage, fast feedback |

---

## Implementation Dependencies

**From Existing Codebase**:
- ✅ PostgreSQL via Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
- ✅ JWT authentication (Microsoft.IdentityModel.JsonWebTokens)
- ✅ Scalar UI for OpenAPI documentation
- ✅ IMemoryCache (ASP.NET Core built-in)
- ✅ xUnit + NSubstitute + FluentAssertions for testing
- ✅ TestContainers for integration tests

**No New External Dependencies Required** - All decisions use existing infrastructure.

---

## Risks and Mitigations

| Risk | Mitigation |
|------|-----------|
| Cache invalidation complexity | Use simple session-scoped keys; invalidate on logout/session timeout |
| Permission check performance | Cache permissions on first access; <50ms target easily achievable |
| Role deletion with active users | Graceful handling: ignore deleted roles, log warning, don't fail request |
| Concurrent role updates | Pessimistic locking not needed; eventual consistency acceptable for permission changes |

---

**Research Complete**: All technical unknowns resolved. Ready for Phase 1: Design.
