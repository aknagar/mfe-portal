using AugmentService.Core.Entities;
using AugmentService.Core.Interfaces;
using AugmentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AugmentService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity operations.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AuthorizationDbContext _context;

    public UserRepository(AuthorizationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));

        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        return user;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        user.UpdatedDate = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
