using AugmentService.Application.DTOs;
using AugmentService.Application.Interfaces;
using AugmentService.Core.Entities;
using AugmentService.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AugmentService.Application.Services;

/// <summary>
/// Implementation of authorization service with permission caching.
/// </summary>
public class AuthorizationService : IPermissionService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthorizationService> _logger;

    // Cache configuration
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromHours(8);
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(30);

    public AuthorizationService(
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IMemoryCache cache,
        ILogger<AuthorizationService> logger)
    {
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<UserPermissionsDto> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out UserPermissionsDto? cachedDto))
        {
            _logger.LogDebug("Cache hit for user {UserId} permissions", userId);
            return cachedDto!;
        }

        _logger.LogDebug("Cache miss for user {UserId} permissions - fetching from database", userId);

        // Fetch from database
        var startTime = DateTime.UtcNow;
        var roles = await _userRoleRepository.GetUserRolesAsync(userId, cancellationToken);
        var permissions = await _userRoleRepository.GetUserPermissionsAsync(userId, cancellationToken);

        var rolesList = roles.ToList();
        var permissionsList = permissions.ToList();

        var dto = new UserPermissionsDto
        {
            UserId = userId,
            Roles = rolesList.Select(MapRoleToDto).ToList(),
            Permissions = permissionsList
        };

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(AbsoluteExpiration)
            .SetSlidingExpiration(SlidingExpiration);

        _cache.Set(cacheKey, dto, cacheOptions);

        var elapsed = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Fetched permissions for user {UserId}: {RoleCount} roles, {PermissionCount} permissions (elapsed: {ElapsedMs}ms)",
            userId, rolesList.Count, permissionsList.Count, elapsed.TotalMilliseconds);

        return dto;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));

        _logger.LogDebug("Checking if user {UserId} has permission {Permission}", userId, permission);

        // Leverage cached permissions via GetUserPermissionsAsync
        var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        var hasPermission = userPermissions.Permissions.Contains(permission);

        _logger.LogDebug("User {UserId} {HasPermission} permission {Permission}",
            userId, hasPermission ? "has" : "lacks", permission);

        return hasPermission;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all active roles");

        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var roleDtos = roles.Select(MapRoleToDto).ToList();

        _logger.LogInformation("Fetched {RoleCount} active roles", roleDtos.Count);

        return roleDtos;
    }

    /// <summary>
    /// Maps a Role entity to RoleDto.
    /// </summary>
    private static RoleDto MapRoleToDto(Role role)
    {
        return new RoleDto
        {
            RoleId = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = role.Permissions.ToList(),
            Rank = role.Rank
        };
    }

    /// <summary>
    /// Generates cache key for user permissions.
    /// Pattern: "permissions:{userId}"
    /// </summary>
    private static string GetCacheKey(Guid userId)
    {
        return $"permissions:{userId}";
    }
}
