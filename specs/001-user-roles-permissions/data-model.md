# Data Model: User Roles and Permissions

**Feature**: 001-user-roles-permissions  
**Date**: January 26, 2026  
**Purpose**: Define domain entities, relationships, and validation rules

---

## Entity Relationship Diagram

```text
┌─────────────────────┐
│       User          │
│  (External Auth)    │
│                     │
│ - UserId: Guid      │
└──────────┬──────────┘
           │
           │ 1
           │
           │ N
      ┌────┴──────────────────┐
      │     UserRole          │
      │  (Join Entity)        │
      │                       │
      │ - Id: Guid            │
      │ - UserId: Guid (FK)   │
      │ - RoleId: Guid (FK)   │
      │ - CreatedDate: DateTime│
      └──────────┬────────────┘
                 │
                 │ N
                 │
                 │ 1
         ┌───────┴───────────────────────┐
         │        Role                   │
         │                               │
         │ - Id: Guid (PK)              │
         │ - Name: string               │
         │ - Description: string        │
         │ - Permissions: List<string>  │
         │ - IsActive: bool             │
         │ - CreatedDate: DateTime      │
         │ - UpdatedDate: DateTime?     │
         └───────────────────────────────┘

Relationship: Many-to-Many (User-Role)
- One User can have multiple Roles
- One Role can be assigned to multiple Users
- One Role has multiple Permissions (stored as JSON array)
- UserRole is the join table
- Permissions are strings like "Augment.Read", "Augment.Write"
```

---

## Entity Definitions

### 1. Role Entity

**Purpose**: Represents a named collection of permissions that can be assigned to users

**Location**: `backend/AugmentService/AugmentService.Core/Entities/Role.cs`

**Code Location for Role Definitions**: `backend/AugmentService/AugmentService.Core/Permissions.cs` (static class with role definitions)

**Properties**:

| Property | Type | Required | Default | Validation | Description |
|----------|------|----------|---------|------------|-------------|
| Id | Guid | Yes | NewGuid() | - | Primary key (inherited from BaseEntity) |
| Name | string | Yes | - | MaxLength(50), Unique | Role name: "Reader", "Writer", "Administrator" |
| Description | string | Yes | - | MaxLength(200) | Human-readable description of the role |
| Permissions | List<string> | Yes | Empty list | Each permission follows pattern "Resource.Action" | List of permissions granted by this role |
| Rank | int | Yes | 1 | Min(1), Max(999) | Role hierarchy rank; higher rank takes precedence when user has multiple roles |
| IsActive | bool | Yes | true | - | Soft delete flag; inactive roles are ignored |
| CreatedDate | DateTime | Yes | UtcNow | - | When role was created (inherited from BaseEntity) |
| UpdatedDate | DateTime? | No | null | - | Last update timestamp (inherited from BaseEntity) |

**Navigation Properties**:
- `ICollection<UserRole> UserRoles` - Users assigned to this role

**Validation Rules**:
- Name must be unique across all roles
- Permissions list must contain at least one permission
- Each permission must follow naming pattern "Resource.Action" (e.g., "Augment.Read")
- Name and Description cannot be null or empty

**EF Core Storage**: 
- Permissions stored as JSON column in PostgreSQL
- Column type: `jsonb` for efficient querying

**Seed Data** (created via EF Core migration, defined in Permissions.cs):
```csharp
{
    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
    Name = "Reader",
    Description = "Read-only access to resources",
    Permissions = new List<string> { "System.Read" },
    Rank = 1,
    IsActive = true
},
{
    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
    Name = "Writer",
    Description = "Read and write access to resources",
    Permissions = new List<string> { "System.Read", "System.Write" },
    Rank = 50,
    IsActive = true
},
{
    Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
    Name = "Administrator",
    Description = "Full administrative access",
    Permissions = new List<string> { "System.Read", "System.Write", "System.Admin" },
    Rank = 999,
    IsActive = true
}
```

---

### 2. UserRole Entity (Join Table)

**Purpose**: Represents the assignment of a role to a user

**Location**: `backend/AugmentService/AugmentService.Core/Entities/UserRole.cs`

**Properties**:

| Property | Type | Required | Default | Validation | Description |
|----------|------|----------|---------|------------|-------------|
| Id | Guid | Yes | NewGuid() | - | Primary key (inherited from BaseEntity) |
| UserId | Guid | Yes | - | Foreign Key | User identifier from authentication system |
| RoleId | Guid | Yes | - | Foreign Key to Role.Id | Assigned role |
| CreatedDate | DateTime | Yes | UtcNow | - | When assignment was created (inherited from BaseEntity) |
| UpdatedDate | DateTime? | No | null | - | Last update timestamp (inherited from BaseEntity) |

**Navigation Properties**:
- `Role Role` - The role assigned to the user

**Validation Rules**:
- UserId and RoleId combination must be unique (composite unique index)
- RoleId must reference an existing, active Role
- Cannot create UserRole for deleted/inactive roles

**Indexes**:
- Composite unique index on (UserId, RoleId) to prevent duplicate assignments
- Index on UserId for fast user permission lookups

---

### 3. User Entity

**Purpose**: Represents an authenticated user in the system

**Location**: `backend/AugmentService/AugmentService.Core/Entities/User.cs`

**Properties**:

| Property | Type | Required | Default | Validation | Description |
|----------|------|----------|---------|------------|-------------|
| UserId | Guid | Yes | NewGuid() | - | Primary key (user identifier) |
| Email | string | Yes | - | MaxLength(256), Email format | User's email address |
| CreatedDate | DateTime | Yes | UtcNow | - | When user was created (inherited from BaseEntity) |
| UpdatedDate | DateTime? | No | null | - | Last update timestamp (inherited from BaseEntity) |

**Navigation Properties**:
- `ICollection<UserRole> UserRoles` - Roles assigned to this user

**Validation Rules**:
- Email must be valid email format
- Email should be unique (recommended index)
- UserId references authentication system user ID

**Business Rules**:
- When user has multiple roles, the role with highest Rank value takes precedence for display/primary role
- Permissions are aggregated from all roles (union)
- User must exist before role assignment

---

### 4. Permission (String Value - Stored in Role)

**Purpose**: Represents a specific capability granted by roles

**Location**: Defined in `backend/AugmentService/AugmentService.Core/Permissions.cs`

**Implementation**: Permissions are string values stored as a JSON array in the Role entity

**Naming Convention**: `"Resource.Action"` pattern
- Resource: The domain/feature being accessed (e.g., "System", "User", "Report")
- Action: The operation being performed (e.g., "Read", "Write", "Delete", "Admin")

**Example Permissions**:
- `"System.Read"` - Read access to system resources
- `"System.Write"` - Write access to system resources
- `"System.Admin"` - Administrative access to system resources
- `"User.Manage"` - Manage user accounts
- `"Report.Generate"` - Generate reports

**Business Rules**:
- One role can have multiple permissions (1:N relationship)
- Users can have multiple permissions by having multiple roles
- Permission aggregation uses **union** logic: User with Reader + Writer roles has all unique permissions from both roles
- Permissions are case-sensitive
- Permission strings are validated against pattern at entity level

---

### 4. Permissions.cs File Structure

**Purpose**: Centralized definition of all roles and their permissions

**Location**: `backend/AugmentService/AugmentService.Core/Permissions.cs`

**Example Structure**:
```csharp
namespace AugmentService.Core;

/// <summary>
/// Centralized permission and role definitions
/// </summary>
public static class Permissions
{
    // Permission constants
    public static class System
    {
        public const string Read = "System.Read";
        public const string Write = "System.Write";
        public const string Admin = "System.Admin";
    }
    
    // Role definitions for seeding
    public static class Roles
    {
        public static readonly Role Reader = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Reader",
            Description = "Read-only access to resources",
            Permissions = new List<string> { System.Read },
            Rank = 1,
            IsActive = true
        };
        
        public static readonly Role Writer = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Name = "Writer",
            Description = "Read and write access to resources",
            Permissions = new List<string> { System.Read, System.Write },
            Rank = 50,
            IsActive = true
        };
        
        public static readonly Role Administrator = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Name = "Administrator",
            Description = "Full administrative access",
            Permissions = new List<string> { System.Read, System.Write, System.Admin },
            Rank = 999,
            IsActive = true
        };
        
        public static IEnumerable<Role> GetAllRoles()
        {
            yield return Reader;
            yield return Writer;
            yield return Administrator;
        }
    }
}
```

**Benefits**:
- Type-safe permission strings (compile-time checking)
- Single source of truth for role definitions
- Easy to extend with new permissions/roles
- Used by both seeding and runtime permission checks

---

## Database Schema

### Tables

#### `Users` Table

```sql
CREATE TABLE "Users" (
    "UserId" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Email" VARCHAR(256) NOT NULL,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMP NULL,
    
    CONSTRAINT "UQ_Users_Email" UNIQUE ("Email")
);

CREATE INDEX "IX_Users_Email" ON "Users" ("Email");
```

#### `Roles` Table

```sql
CREATE TABLE "Roles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(200) NOT NULL,
    "Permissions" JSONB NOT NULL DEFAULT '[]',
    "Rank" INTEGER NOT NULL DEFAULT 1,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMP NULL,
    
    CONSTRAINT "CHK_Roles_Rank" CHECK ("Rank" >= 1 AND "Rank" <= 999)
);

CREATE INDEX "IX_Roles_IsActive" ON "Roles" ("IsActive");
CREATE INDEX "IX_Roles_Permissions" ON "Roles" USING GIN ("Permissions");
CREATE INDEX "IX_Roles_Rank" ON "Roles" ("Rank" DESC);
```

**Notes**:
- `Permissions` is a JSONB column storing an array of permission strings
- GIN index on Permissions enables efficient permission lookups
- Example data: `{"Permissions": ["Augment.Read", "Augment.Write"]}`

#### `UserRoles` Table

```sql
CREATE TABLE "UserRoles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL,
    "RoleId" UUID NOT NULL,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMP NULL,
    
    CONSTRAINT "FK_UserRoles_Users" FOREIGN KEY ("UserId") 
        REFERENCES "Users" ("UserId") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRoles_Roles" FOREIGN KEY ("RoleId") 
        REFERENCES "Roles" ("Id") ON DELETE CASCADE,
    
    CONSTRAINT "UQ_UserRoles_UserId_RoleId" UNIQUE ("UserId", "RoleId")
);

CREATE INDEX "IX_UserRoles_UserId" ON "UserRoles" ("UserId");
CREATE INDEX "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");
```

---

## Data Access Patterns

### Query Patterns

#### Get User Permissions
```csharp
// Retrieve all active permissions for a user (aggregated from all roles)
var permissions = await _context.UserRoles
    .Where(ur => ur.UserId == userId)
    .Include(ur => ur.Role)
    .Where(ur => ur.Role.IsActive)
    .SelectMany(ur => ur.Role.Permissions)
    .Distinct()
    .ToListAsync(cancellationToken);
```

#### Get User Roles
```csharp
// Retrieve all active roles for a user
var roles = await _context.UserRoles
    .Where(ur => ur.UserId == userId)
    .Include(ur => ur.Role)
    .Where(ur => ur.Role.IsActive)
    .Select(ur => ur.Role)
    .ToListAsync(cancellationToken);

// Get primary role (highest rank)
var primaryRole = await _context.UserRoles
    .Where(ur => ur.UserId == userId)
    .Include(ur => ur.Role)
    .Where(ur => ur.Role.IsActive)
    .OrderByDescending(ur => ur.Role.Rank)
    .Select(ur => ur.Role)
    .FirstOrDefaultAsync(cancellationToken);
```

#### Check Specific Permission
```csharp
// Fast permission check using JSONB containment
var hasPermission = await _context.UserRoles
    .AnyAsync(ur => 
        ur.UserId == userId && 
        ur.Role.IsActive && 
        ur.Role.Permissions.Contains(permissionName),
        cancellationToken);

// Alternative: EF Core translates Contains to PostgreSQL @> operator for JSONB
```

#### Get All Roles (Admin Only)
```csharp
// List all active roles with their permissions
var roles = await _context.Roles
    .Where(r => r.IsActive)
    .OrderBy(r => r.Name)
    .ToListAsync(cancellationToken);
```

---

## State Transitions

### Role Lifecycle

```
[New] → [Active] → [Inactive/Deleted]
  ↓         ↓            ↓
Create   Assign to    Stop assigning
         Users        to new users
```

**States**:
1. **New**: Role created but not yet assigned
2. **Active**: Role in use, can be assigned to users
3. **Inactive**: Soft-deleted, ignored in permission checks but historical assignments remain

**Transitions**:
- Create → Active (IsActive = true by default)
- Active → Inactive (IsActive = false)
- Inactive → Active (Can be re-activated)

### UserRole Lifecycle

```
[Not Assigned] → [Assigned] → [Revoked]
                     ↓
                Permissions
                Available
```

**States**:
1. **Not Assigned**: User does not have the role
2. **Assigned**: UserRole record exists, permissions active
3. **Revoked**: UserRole record deleted, permissions removed

**Transitions**:
- Assign: Create UserRole record
- Revoke: Delete UserRole record
- Role Deactivated: Permissions ignored but assignment remains

---

## Validation Matrix

| Entity | Field | Validation | Error Message |
|--------|-------|------------|---------------|
| User | UserId | Required, Valid Guid | "User ID is required" |
| User | Email | Required, MaxLength(256), Email format, Unique | "Valid email address is required" |
| Role | Name | Required, MaxLength(50), Unique | "Role name is required and must be unique" |
| Role | Description | Required, MaxLength(200) | "Role description is required" |
| Role | Permissions | Required, MinCount(1), Pattern("^[A-Za-z]+\\.[A-Za-z]+$") | "At least one permission required, each must follow 'Resource.Action' format" |
| Role | Rank | Required, Min(1), Max(999) | "Rank must be between 1 and 999" |
| UserRole | UserId | Required, Valid Guid, Must exist in Users | "User ID is required and must exist" |
| UserRole | RoleId | Required, Valid Guid, Must exist in Roles | "Role ID is required and must exist" |
| UserRole | Duplicate | Unique (UserId, RoleId) | "User already has this role assigned" |

---

## Performance Considerations

### Caching Strategy

**What**: Cache user permissions after first retrieval  
**Key Pattern**: `permissions:{userId}:{sessionId}`  
**TTL**: Session duration (8 hours absolute, 30 minutes sliding)  
**Invalidation**: On session logout or expiration

**Expected Performance**:
- First call: ~50-100ms (DB query + cache write)
- Cached calls: <5ms (in-memory read)
- Cache hit rate: >95%

### Index Strategy

**Primary Indexes**:
- `IX_UserRoles_UserId` - Fast user permission lookups
- `IX_UserRoles_RoleId` - Fast role membership queries
- `IX_Roles_IsActive` - Filter active roles
- `UQ_UserRoles_UserId_RoleId` - Prevent duplicate assignments

**Query Optimization**:
- Include navigation properties in single query (avoid N+1)
- Use `AnyAsync` for boolean checks instead of `CountAsync`
- Filter inactive roles at database level, not in-memory

---

## Migration Strategy

### Initial Migration

**Name**: `20260126_AddRolesAndPermissions`

**Up** (creates schema):
```csharp
migrationBuilder.CreateTable(
    name: "Roles",
    columns: table => new
    {
        Id = table.Column<Guid>(),
        Name = table.Column<string>(maxLength: 50),
        Description = table.Column<string>(maxLength: 200),
        Permissions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
        IsActive = table.Column<bool>(defaultValue: true),
        CreatedDate = table.Column<DateTime>(),
        UpdatedDate = table.Column<DateTime>(nullable: true)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Roles", x => x.Id);
    });

// Create GIN index for JSONB column
migrationBuilder.Sql(
    "CREATE INDEX \"IX_Roles_Permissions\" ON \"Roles\" USING GIN (\"Permissions\")");

migrationBuilder.CreateTable(
    name: "UserRoles",
    columns: table => new
    {
        Id = table.Column<Guid>(),
        UserId = table.Column<Guid>(),
        RoleId = table.Column<Guid>(),
        CreatedDate = table.Column<DateTime>(),
        UpdatedDate = table.Column<DateTime>(nullable: true)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_UserRoles", x => x.Id);
        table.ForeignKey("FK_UserRoles_Roles", x => x.RoleId, "Roles", "Id");
    });

// Seed data for three roles with permissions arrays
migrationBuilder.InsertData(...);
```

**Down** (rollback):
```csharp
migrationBuilder.DropTable(name: "UserRoles");
migrationBuilder.DropTable(name: "Roles");
```

---

## Future Extensibility

### Potential Additions (Out of Scope for MVP)

1. **Permission Hierarchy**: Admin includes Write, Write includes Read
2. **Role Metadata**: Cost center, department, expiration date
3. **Permission Grants**: Granular permissions beyond Read/Write/Admin
4. **Audit Trail**: Track who assigned/revoked roles and when
5. **Temporal Roles**: Time-limited role assignments
6. **Role Groups**: Group roles for easier bulk assignment

**Design Principle**: Current model supports these extensions without breaking changes by:
- Using explicit join entity (UserRole) that can have metadata added
- Soft deletes (IsActive) instead of hard deletes
- Normalized structure that can add new tables/relationships

---

**Data Model Complete**: All entities defined, relationships mapped, ready for implementation.
