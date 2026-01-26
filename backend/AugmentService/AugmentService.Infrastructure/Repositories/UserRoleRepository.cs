using AugmentService.Core.Entities;
using AugmentService.Core.Interfaces;
using AugmentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AugmentService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserRole entity operations.
/// Handles user-role assignments and permission queries.
/// </summary>
public class UserRoleRepository : IUserRoleRepository
{
    private readonly AuthorizationDbContext _context;

    public UserRoleRepository(AuthorizationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role)
            .OrderByDescending(r => r.Rank)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var permissions = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.IsActive)
            .SelectMany(ur => ur.Role.Permissions)
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));

        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.IsActive)
            .AnyAsync(ur => ur.Role.Permissions.Contains(permission), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> GetPrimaryRoleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.IsActive)
            .OrderByDescending(ur => ur.Role.Rank)
            .Select(ur => ur.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserRole> AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        if (userRole == null)
            throw new ArgumentNullException(nameof(userRole));

        // Check if assignment already exists
        var exists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userRole.UserId && ur.RoleId == userRole.RoleId, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"User {userRole.UserId} is already assigned to role {userRole.RoleId}.");

        await _context.UserRoles.AddAsync(userRole, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        return userRole;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
