using AugmentService.Core.Entities;

namespace AugmentService.Core.Interfaces;

/// <summary>
/// Repository interface for User entity operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user to the repository.
    /// </summary>
    /// <param name="user">The user to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added user.</returns>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
