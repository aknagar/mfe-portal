# Quickstart Guide: User Roles and Permissions

**Feature**: 001-user-roles-permissions  
**Audience**: Backend developers implementing or consuming the authorization API  
**Time to Complete**: 15 minutes

---

## Prerequisites

Before you begin, ensure you have:
- ✅ .NET 10.0 SDK installed
- ✅ PostgreSQL accessible (local or via Docker/Aspire)
- ✅ Visual Studio 2022 or VS Code with C# extension
- ✅ Basic understanding of Clean Architecture and ASP.NET Core

---

## Quick Overview

This feature provides three REST API endpoints for user authorization:

1. **GET /api/authorization/my-permissions** - Get current user's roles and permissions
2. **POST /api/authorization/check-permission** - Check if user has specific permission
3. **GET /api/authorization/roles** - List all roles (admin only)

**Supported Roles & Permissions**:
- `Reader` → `["System.Read"]` (Rank: 1)
- `Writer` → `["System.Read", "System.Write"]` (Rank: 50)
- `Administrator` → `["System.Read", "System.Write", "System.Admin"]` (Rank: 999)

**Permission Naming**: Follows "Resource.Action" pattern (e.g., "System.Read")

**Rank System**: When a user has multiple roles, the role with highest rank is the primary role.

---

## Step 1: Run Database Migrations

The feature uses Entity Framework Core with PostgreSQL. Create the schema and seed initial roles:

```bash
# Navigate to API project
cd backend/AugmentService/AugmentService.Api

# Create and apply migration (creates roles and user_roles tables)
dotnet ef migrations add AddRolesAndPermissions --project ../AugmentService.Infrastructure
dotnet ef database update

# Verify migration success
dotnet ef migrations list
```

**Expected Result**: Two new tables created:
- `Roles` (with 3 seeded roles: Reader, Writer, Administrator)
- `UserRoles` (empty, ready for user assignments)

---

## Step 2: Start the Application

Use .NET Aspire to start the full stack:

```bash
# From repository root
cd backend/MfePortal.AppHost
dotnet run

# Or use Visual Studio: Set MfePortal.AppHost as startup project and press F5
```

**Expected Output**:
```
Aspire Dashboard: http://localhost:15000
AugmentService API: https://localhost:7001
```

**Verify Health**:
```bash
curl https://localhost:7001/health
# Should return: Healthy
```

---

## Step 3: Explore the API (Swagger/Scalar UI)

Open the API documentation in your browser:

**Scalar UI** (recommended):
```
https://localhost:7001/scalar/v1
```

**Swagger UI** (alternative):
```
https://localhost:7001/swagger
```

You'll see three new endpoints under the "Authorization" tag:
- `GET /api/authorization/my-permissions`
- `POST /api/authorization/check-permission`
- `GET /api/authorization/roles`

---

## Step 4: Test the Endpoints

### Option A: Using Scalar UI

1. Open `https://localhost:7001/scalar/v1`
2. Click "Authorize" button
3. Enter a test JWT token (see "Getting Auth Token" below)
4. Try each endpoint:
   - **GET /my-permissions**: Click "Send Request"
   - **POST /check-permission**: Enter `{"permission": "Read"}` and send
   - **GET /roles**: Send request (requires Admin permission)

### Option B: Using curl

```bash
# Get user permissions
curl -X GET https://localhost:7001/api/authorization/my-permissions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Check specific permission
curl -X POST https://localhost:7001/api/authorization/check-permission \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"permission": "System.Write"}'

# List all roles (admin only)
curl -X GET https://localhost:7001/api/authorization/roles \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Option C: Using HTTP files (VS Code REST Client)

Create `test-authorization.http`:

```http
@baseUrl = https://localhost:7001
@token = YOUR_JWT_TOKEN

### Get my permissions
GET {{baseUrl}}/api/authorization/my-permissions
Authorization: Bearer {{token}}

### Check Write permission
POST {{baseUrl}}/api/authorization/check-permission
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "permission": "System.Write"
}

### List all roles
GET {{baseUrl}}/api/authorization/roles
Authorization: Bearer {{token}}
```

---

## Step 5: Assign Roles to Users

To test with real data, assign roles to your test user:

```sql
-- Connect to PostgreSQL
psql -h localhost -U postgres -d mfeportal

-- Assign Reader role to user
INSERT INTO "UserRoles" ("Id", "UserId", "RoleId", "CreatedDate")
VALUES (
    gen_random_uuid(),
    'YOUR_USER_ID_FROM_JWT',  -- Replace with actual user ID
    '00000000-0000-0000-0000-000000000001',  -- Reader role
    NOW()
);

-- Verify assignment
SELECT ur."UserId", r."Name", r."Permissions"
FROM "UserRoles" ur
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE ur."UserId" = 'YOUR_USER_ID_FROM_JWT';
```

Now when you call `/my-permissions`, you'll see the Reader role in the response.

---

## Getting an Auth Token

### For Development/Testing

Since authentication is out of scope for this feature, you'll need a valid JWT token from the existing auth system:

**Option 1**: Use existing login endpoint
```bash
# Assuming you have a login endpoint
curl -X POST https://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"password"}'
```

**Option 2**: Generate mock JWT for testing
```csharp
// Add this to a test controller or use a JWT generator tool
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, "123e4567-e89b-12d3-a456-426614174000"),
    new Claim(ClaimTypes.Name, "Test User")
};

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key"));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var token = new JwtSecurityToken(
    issuer: "mfeportal",
    audience: "mfeportal-api",
    claims: claims,
    expires: DateTime.Now.AddHours(1),
    signingCredentials: creds
);

var jwt = new JwtSecurityTokenHandler().WriteToken(token);
```

---

## Common Use Cases

### Use Case 1: Show/Hide UI Features Based on Permissions

**Scenario**: Frontend needs to know if user can edit a document

```typescript
// Frontend TypeScript example
async function checkCanEdit(): Promise<boolean> {
  const response = await fetch('/api/authorization/check-permission', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${jwt}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ permission: 'System.Write' })
  });
  
  const result = await response.json();
  return result.hasPermission;
}

// Use in UI
if (await checkCanEdit()) {
  showEditButton();
}
```

### Use Case 2: Initialize App with User Permissions

**Scenario**: Load permissions on app startup

```typescript
// Frontend TypeScript example
async function initializeApp() {
  const response = await fetch('/api/authorization/my-permissions', {
    headers: { 'Authorization': `Bearer ${jwt}` }
  });
  
  const data = await response.json();
  
  // Store in app state
  appState.permissions = data.permissions;  // ["System.Read", "System.Write"]
  appState.roles = data.roles;              // [{name: "Reader", permissions: ["System.Read"], rank: 1}, ...]
  
  // Configure UI based on permissions
  configureMenus(data.permissions);
}
```

### Use Case 3: Admin Role Management UI

**Scenario**: Admin views all available roles

```typescript
// Frontend TypeScript example (admin only)
async function loadRoles(): Promise<Role[]> {
  const response = await fetch('/api/authorization/roles', {
    headers: { 'Authorization': `Bearer ${jwt}` }
  });
  
  if (response.status === 403) {
    showError('Admin access required');
    return [];
  }
  
  const data = await response.json();
  return data.roles;
}
```

---

## Performance Tips

### Caching Best Practices

The API automatically caches permissions for the session duration:
- **First call**: ~50-100ms (database query)
- **Subsequent calls**: <5ms (cache hit)
- **Cache TTL**: 8 hours absolute, 30 minutes sliding

**Frontend Implementation**:
```typescript
// Call once on login/app start
const permissions = await fetchMyPermissions();

// Store in memory/state for the session
sessionStorage.setItem('permissions', JSON.stringify(permissions));

// Use cached data for UI decisions (no repeated API calls)
function hasPermission(permission: string): boolean {
  const cached = JSON.parse(sessionStorage.getItem('permissions') || '{}');
  return cached.permissions?.includes(permission) || false;
}
```

### When to Refresh Permissions
- ✅ On login
- ✅ On session renewal
- ✅ After role assignment changes (admin operation)
- ❌ NOT on every page navigation
- ❌ NOT on every permission check

---

## Troubleshooting

### Issue: 401 Unauthorized

**Symptom**: All endpoints return 401  
**Cause**: Missing or invalid JWT token  
**Fix**:
1. Verify token is in `Authorization: Bearer {token}` header
2. Check token expiration
3. Verify token signature matches server configuration

```bash
# Debug: Decode JWT to inspect claims
# Use https://jwt.io or jwt-cli tool
```

### Issue: 403 Forbidden on /roles endpoint

**Symptom**: User gets 403 when calling `/api/authorization/roles`  
**Cause**: User doesn't have Admin permission  
**Fix**: Assign Administrator role to user:

```sql
INSERT INTO "UserRoles" ("Id", "UserId", "RoleId", "CreatedDate")
VALUES (
    gen_random_uuid(),
    'YOUR_USER_ID',
    '00000000-0000-0000-0000-000000000003',  -- Administrator role
    NOW()
);
```

### Issue: Empty permissions array

**Symptom**: `/my-permissions` returns `{"permissions": []}`  
**Cause**: User has no roles assigned  
**Fix**: Assign at least one role (see Step 5 above)

### Issue: Database migration fails

**Symptom**: `dotnet ef database update` fails  
**Cause**: PostgreSQL not running or connection string incorrect  
**Fix**:
1. Verify PostgreSQL is running: `docker ps` or check service
2. Check connection string in `appsettings.Development.json`
3. Test connection: `psql -h localhost -U postgres`

---

## Next Steps

### For Frontend Developers
1. Integrate `/my-permissions` call on app initialization
2. Cache permissions in app state/context
3. Implement UI permission checks before showing features
4. Handle 401/403 errors gracefully (redirect to login, show access denied)

### For Backend Developers
1. Use `IAuthorizationService` in your business logic to check permissions
2. Add `[Authorize]` attribute to protected endpoints
3. Implement role assignment endpoints (out of scope for this feature)
4. Add audit logging for permission checks if required

### For Administrators
1. Use `/roles` endpoint to understand the authorization model
2. Create user management UI to assign roles
3. Monitor cache hit rates for performance tuning
4. Plan for future role additions if needed

---

## Code Examples

### Backend: Use AuthorizationService in Your Controller

```csharp
[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IAuthorizationService _authService;

    public DocumentsController(IAuthorizationService authService)
    {
        _authService = authService;
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(Guid id, UpdateDocumentDto dto)
    {
        var userId = GetUserIdFromClaims();
        
        // Check permission before allowing edit
        var canWrite = await _authService.HasPermissionAsync(userId, "System.Write");
        if (!canWrite)
        {
            return Forbid();
        }
        
        // Proceed with update
        // ...
    }
    
    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim);
    }
}
```

### Frontend: React Hook for Permissions

```typescript
// usePermissions.ts
import { useState, useEffect } from 'react';

interface PermissionsData {
  permissions: string[];
  roles: Role[];
  loading: boolean;
  error: string | null;
}

export function usePermissions(): PermissionsData {
  const [data, setData] = useState<PermissionsData>({
    permissions: [],
    roles: [],
    loading: true,
    error: null
  });

  useEffect(() => {
    fetch('/api/authorization/my-permissions', {
      headers: { 'Authorization': `Bearer ${getToken()}` }
    })
      .then(res => res.json())
      .then(result => {
        setData({
          permissions: result.permissions,
          roles: result.roles,
          loading: false,
          error: null
        });
      })
      .catch(err => {
        setData(prev => ({ ...prev, loading: false, error: err.message }));
      });
  }, []);

  return data;
}

// Usage in component
function DocumentEditor() {
  const { permissions } = usePermissions();
  
  if (!permissions.includes('Write')) {
    return <div>You don't have permission to edit documents</div>;
  }
  
  return <DocumentEditForm />;
}
```

---

## API Reference Summary

| Endpoint | Method | Auth | Purpose | Response Time |
|----------|--------|------|---------|---------------|
| `/api/authorization/my-permissions` | GET | Required | Get user's roles & permissions | <5ms (cached), ~50ms (first call) |
| `/api/authorization/check-permission` | POST | Required | Check specific permission | <5ms (cached) |
| `/api/authorization/roles` | GET | Required (Admin) | List all roles | ~20ms |

---

## Additional Resources

- **Full API Documentation**: `https://localhost:7001/scalar/v1`
- **Data Model**: See [data-model.md](data-model.md)
- **API Contracts**: See [contracts/authorization-api.yaml](contracts/authorization-api.yaml)
- **Testing Guide**: See `backend/tests/README.md` (created during implementation)

---

**Questions?** Check the feature specification in [spec.md](spec.md) or implementation plan in [plan.md](plan.md).
