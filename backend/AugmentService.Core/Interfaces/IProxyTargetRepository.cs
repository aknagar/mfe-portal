using AugmentService.Core.Entities;

namespace AugmentService.Core.Interfaces;

/// <summary>
/// Repository interface for ProxyTarget entities.
/// </summary>
public interface IProxyTargetRepository
{
    Task<ProxyTarget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProxyTarget?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProxyTarget>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProxyTarget> AddAsync(ProxyTarget target, CancellationToken cancellationToken = default);
    Task<ProxyTarget> UpdateAsync(ProxyTarget target, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
