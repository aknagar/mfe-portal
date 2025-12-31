using AugmentService.Core.Entities;
using AugmentService.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AugmentService.Infrastructure.Repositories;

/// <summary>
/// In-memory repository implementation for ProxyTarget entities.
/// Replace with EF Core or other persistence mechanism as needed.
/// </summary>
public class InMemoryProxyTargetRepository : IProxyTargetRepository
{
    private readonly Dictionary<Guid, ProxyTarget> _targets = [];
    private readonly ILogger<InMemoryProxyTargetRepository> _logger;

    public InMemoryProxyTargetRepository(ILogger<InMemoryProxyTargetRepository> logger)
    {
        _logger = logger;
    }

    public Task<ProxyTarget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _targets.TryGetValue(id, out var target);
        return Task.FromResult(target);
    }

    public Task<ProxyTarget?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var target = _targets.Values.FirstOrDefault(t => t.Name == name && !t.IsDeleted);
        return Task.FromResult(target);
    }

    public Task<IEnumerable<ProxyTarget>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var targets = _targets.Values.Where(t => !t.IsDeleted).AsEnumerable();
        return Task.FromResult(targets);
    }

    public Task<ProxyTarget> AddAsync(ProxyTarget target, CancellationToken cancellationToken = default)
    {
        target.Id = Guid.NewGuid();
        target.CreatedAt = DateTime.UtcNow;
        _targets[target.Id] = target;
        _logger.LogInformation("ProxyTarget added: {TargetName}", target.Name);
        return Task.FromResult(target);
    }

    public Task<ProxyTarget> UpdateAsync(ProxyTarget target, CancellationToken cancellationToken = default)
    {
        if (!_targets.ContainsKey(target.Id))
        {
            throw new KeyNotFoundException($"ProxyTarget with ID {target.Id} not found");
        }
        
        target.UpdatedAt = DateTime.UtcNow;
        _targets[target.Id] = target;
        _logger.LogInformation("ProxyTarget updated: {TargetName}", target.Name);
        return Task.FromResult(target);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_targets.TryGetValue(id, out var target))
        {
            return Task.FromResult(false);
        }

        target.IsDeleted = true;
        target.UpdatedAt = DateTime.UtcNow;
        _logger.LogInformation("ProxyTarget soft-deleted: {Id}", id);
        return Task.FromResult(true);
    }
}
