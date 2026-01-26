using AugmentService.Core.Entities;

namespace AugmentService.Core.Interfaces;

/// <summary>
/// Repository interface for Role entity operations.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Retrieves a role by its unique identifier.
    /// </summary>
    /// <param name="id">The role's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role if found, null otherwise.</returns>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a role by its name.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role if found, null otherwise.</returns>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active roles ordered by name.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of all active roles.</returns>
    Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new role to the repository.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added role.</returns>
    Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default);
}
