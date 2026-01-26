using AugmentService.Core.Entities;
using AugmentService.Core.Interfaces;
using AugmentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AugmentService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Role entity operations.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly AuthorizationDbContext _context;

    public RoleRepository(AuthorizationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty.", nameof(name));

        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name && r.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        await _context.Roles.AddAsync(role, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        return role;
    }
}
