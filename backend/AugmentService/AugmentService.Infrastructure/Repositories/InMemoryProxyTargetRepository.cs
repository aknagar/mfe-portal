using AugmentService.Core.Entities;
using AugmentService.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AugmentService.Infrastructure.Repositories;

public class InMemoryProxyTargetRepository : IProxyTargetRepository
{
    private readonly Dictionary<Guid, ProxyTarget> _targets = [];
    private readonly ILogger<InMemoryProxyTargetRepository> _logger;

    public InMemoryProxyTargetRepository(ILogger<InMemoryProxyTargetRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ProxyTarget> AddAsync(ProxyTarget target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        _targets[target.Id] = target;
        _logger.LogInformation("Added proxy target: {Name} ({Id})", target.Name, target.Id);
        return Task.FromResult(target);
    }

    public Task<ProxyTarget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _targets.TryGetValue(id, out var target);
        return Task.FromResult(target);
    }

    public Task<IEnumerable<ProxyTarget>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_targets.Values.AsEnumerable());
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _targets.Remove(id);
        if (removed)
        {
            _logger.LogInformation("Deleted proxy target: {Id}", id);
        }
        return Task.FromResult(removed);
    }

    public Task<ProxyTarget?> UpdateAsync(ProxyTarget target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (_targets.ContainsKey(target.Id))
        {
            _targets[target.Id] = target;
            _logger.LogInformation("Updated proxy target: {Name} ({Id})", target.Name, target.Id);
            return Task.FromResult<ProxyTarget?>(target);
        }

        return Task.FromResult<ProxyTarget?>(null);
    }
}
