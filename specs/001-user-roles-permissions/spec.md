# Feature Specification: User Roles and Permissions Component

**Feature Branch**: `001-user-roles-permissions`  
**Created**: January 26, 2026  
**Status**: Draft  
**Input**: User description: "I have to implement a component for providing roles and permissions for the logged-in user"

## Clarifications

### Session 2026-01-26

- Q: What should happen when a non-admin user tries to view the list of available roles? → A: Return authorization error (403 Forbidden)
- Q: What specific roles and permissions should the backend service support? → A: Three roles with multiple permissions each. Reader: ["System.Read"] (Rank: 1), Writer: ["System.Read", "System.Write"] (Rank: 50), Administrator: ["System.Read", "System.Write", "System.Admin"] (Rank: 999). Permissions follow "Resource.Action" pattern. Role definitions go in Permissions.cs file. When user has multiple roles, highest rank takes precedence.
- Q: When should role/permission changes take effect for an active user session? → A: Take effect on next login (session unchanged)
- Q: How should the system handle users with references to deleted roles? → A: Treat deleted roles as having no permissions (ignore gracefully)
- Q: How should permission checks behave when the user's session has expired? → A: Return 401 Unauthorized error (requires re-authentication)
- Q: What authentication mechanism should the API use to identify and authenticate users? → A: Azure AD / Entra ID (OpenID Connect)
- Q: How should Azure AD user identities be mapped to role assignments in the system? → A: Email address
- Q: Where should role assignments and role definitions be persisted? → A: PostgreSQL database
- Q: What should happen when an Azure AD user authenticates but doesn't have a record in the PostgreSQL database yet? → A: Auto-provision user record
- Q: How should permission caching be implemented to reduce database queries? → A: In-memory cache
- Q: What should the API controller be named for the roles and permissions endpoints? → A: UserController

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrieve My Roles and Permissions (Priority: P1)

As an authenticated user, when I access the application, I need to know what roles I have and what actions I'm permitted to perform so that the UI can show/hide features appropriately and the system can enforce access control.

**Why this priority**: This is the core MVP functionality - without the ability to query current user's roles and permissions, no authorization enforcement is possible. All other authorization features depend on this foundation.

**Independent Test**: Can be fully tested by authenticating a user with specific roles, calling the API endpoint to retrieve their permissions, and verifying the returned data matches their assigned roles and permissions. Delivers immediate value by enabling basic permission checks in the UI.

**Acceptance Scenarios**:

1. **Given** I am logged in as a user with "Administrator" role, **When** I request my current roles and permissions, **Then** the system returns my "Administrator" role with permissions ["System.Read", "System.Write", "System.Admin"] and rank 999
2. **Given** I am logged in as a user with multiple roles ("Reader" and "Writer"), **When** I request my permissions, **Then** the system returns the combined set containing ["System.Read", "System.Write"] and the primary role is "Writer" (highest rank: 50)
3. **Given** I am logged in but have no roles assigned (or I'm a new user auto-provisioned on first login), **When** I request my permissions, **Then** the system returns an empty permissions set with appropriate status
4. **Given** I am not authenticated, **When** I attempt to request my permissions, **Then** the system returns an authentication error

---

### User Story 2 - Check Specific Permission (Priority: P2)

As an authenticated user or UI component, I need to check if I have a specific permission (e.g., "System.Write") so that I can make granular access control decisions without analyzing the entire permission set.

**Why this priority**: This provides a convenient helper for common authorization checks. While P1 provides all data needed, this improves developer experience and reduces client-side logic duplication.

**Independent Test**: Can be tested by authenticating users with different role combinations, calling the permission check endpoint with various permission names, and verifying correct true/false responses. Delivers value by simplifying permission checks across the application.

**Acceptance Scenarios**:

1. **Given** I have the "System.Write" permission, **When** I check if I have that permission, **Then** the system returns true
2. **Given** I do not have the "System.Admin" permission, **When** I check if I have that permission, **Then** the system returns false
3. **Given** I check for a permission that doesn't exist in the system, **When** I perform the check, **Then** the system returns false (not an error)

---

### User Story 3 - View Available Roles (Priority: P3)

As a system administrator, I need to see what roles exist in the system and their associated permissions so that I understand the access control model and can assign appropriate roles to users.

**Why this priority**: This is administrative functionality that supports role management but isn't required for basic authorization enforcement. Useful for transparency and role assignment workflows.

**Independent Test**: Can be tested by calling the roles listing endpoint and verifying it returns all configured roles with their permission sets. Delivers value by providing visibility into the authorization model.

**Acceptance Scenarios**:

1. **Given** I am an administrator, **When** I request the list of available roles, **Then** the system returns all roles with their names, descriptions, and associated permissions
2. **Given** I am a regular user without admin privileges, **When** I request the list of available roles, **Then** the system returns a 403 Forbidden error

---

### Edge Cases

- What happens when an Azure AD user logs in for the first time? The system automatically creates a user record in the database with no roles assigned (auto-provisioning), allowing the user to authenticate successfully but receive empty permissions until an administrator assigns roles.
- What happens when a user's roles are modified while they are logged in? Role changes take effect only on the user's next login; the current session maintains its original permissions for consistency.
- How does the system handle permission checks for resources that don't exist?
- What happens if a role is deleted but users still have that role assigned? The system treats deleted roles as having no permissions (ignores them gracefully) and continues processing remaining valid roles.
- How does the system handle concurrent role/permission updates?
- What happens when checking permissions for a user whose session has expired? The system returns a 401 Unauthorized error, requiring the user to re-authenticate.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an API endpoint that returns the authenticated user's current roles and associated permissions
- **FR-002**: System MUST aggregate permissions from multiple roles when a user has more than one role assigned (union of all permissions)
- **FR-003**: System MUST support permission naming using a namespace pattern "Resource.Action" (e.g., "System.Read", "User.Manage"), and one role can have multiple permissions
- **FR-004**: System MUST provide a helper method/endpoint to check if the current user has a specific named permission
- **FR-005**: System MUST handle unauthenticated requests and expired sessions by returning appropriate error responses (401 Unauthorized)
- **FR-006**: System MUST handle users with no assigned roles by returning an empty permission set (not an error)
- **FR-007**: System MUST provide an endpoint to list all available roles in the system with their associated permissions (restricted to administrator users only)
- **FR-008**: System MUST cache user permissions using ASP.NET Core IMemoryCache with per-user cache keys for the duration of their session (configurable timeout, default 60 minutes) to avoid repeated database lookups
- **FR-009**: System MUST validate that role assignments exist and are active when returning user permissions; deleted or inactive roles are ignored gracefully (treated as having no permissions)
- **FR-010**: System MUST return permission data in a structured, machine-readable format (e.g., JSON)
- **FR-011**: System MUST maintain consistent permissions throughout a user's session; role changes take effect only on next login
- **FR-012**: System MUST use Azure AD / Entra ID (OpenID Connect) for user authentication, validating JWT bearer tokens on all protected endpoints
- **FR-013**: System MUST persist user-to-role assignments in PostgreSQL database tables, enabling dynamic role management without requiring Azure AD configuration changes
- **FR-014**: System MUST automatically create a user record in the database on first login if the authenticated Azure AD user doesn't exist (auto-provisioning), with no roles assigned initially (empty permissions)

### Technical Constraints

- **TC-001**: Authentication mechanism is Azure AD / Entra ID using OpenID Connect protocol with JWT bearer tokens
- **TC-002**: All API endpoints requiring user context must enforce authentication via ASP.NET Core authentication middleware
- **TC-003**: User identity claims (including user ID and email) are extracted from validated Azure AD JWT tokens
- **TC-004**: User email address from Azure AD token claims is used to map authenticated users to role assignments stored in the application database
- **TC-005**: Role assignments and role definitions are persisted in PostgreSQL database with tables for Users, Roles, and UserRoles entities
- **TC-006**: Permission caching uses ASP.NET Core IMemoryCache for in-memory storage with per-user cache keys, entries expire after configurable timeout (default 60 minutes)
- **TC-007**: API endpoints for roles and permissions are exposed through a controller named UserController (not AuthorizationController) to reflect user-centric operations

### Key Entities *(include if feature involves data)*

- **User**: Represents an authenticated individual who can have one or more roles assigned. The user's email address from the Azure AD token is used as the primary identifier to link authentication identity to role assignments in the database. User records are automatically created on first login if they don't exist (auto-provisioned with no roles). Key attributes include UserId (Guid - internal database ID), Email (string - from Azure AD token, used for identity matching), authentication status.
- **Role**: Represents a named collection of permissions that can be assigned to users. The system supports three roles: Reader (grants System.Read permission, Rank: 1), Writer (grants System.Read and System.Write permissions, Rank: 50), and Administrator (grants System.Read, System.Write, and System.Admin permissions, Rank: 999). Key attributes include role name, description, permissions list, and rank for hierarchy. A user can have multiple roles, with the highest rank role being the primary role. Role definitions are defined in Permissions.cs file.
- **Permission**: Represents a specific capability or action that can be performed in the system. Permissions follow a naming pattern "Resource.Action" (e.g., "System.Read", "User.Manage"). One role can have multiple permissions stored as a list.
- **UserRole**: Represents the assignment relationship between a user and a role, linking users to their authorized roles. Stored in PostgreSQL database as a many-to-many relationship table with foreign keys to Users and Roles tables.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Authenticated users can retrieve their complete permission set in under 200 milliseconds on average
- **SC-002**: Permission check operations complete in under 50 milliseconds to support real-time UI authorization decisions
- **SC-003**: System correctly handles 100 concurrent permission requests without errors or degraded performance
- **SC-004**: Permission caching reduces database queries for repeat permission checks by at least 90% during a user session
- **SC-005**: API responses use consistent, well-documented data structures that frontend developers can integrate without clarification
- **SC-006**: Zero authentication bypass incidents - unauthenticated requests always receive appropriate error responses
