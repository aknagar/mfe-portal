using AugmentService.Core.Entities;

namespace AugmentService.Core.Interfaces;

public interface IProxyTargetRepository
{
    Task<ProxyTarget> AddAsync(ProxyTarget target, CancellationToken cancellationToken = default);
    Task<ProxyTarget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProxyTarget>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProxyTarget?> UpdateAsync(ProxyTarget target, CancellationToken cancellationToken = default);
}
