namespace MfePortal.Core.Entities;

/// <summary>
/// Represents a remote HTTP endpoint to be proxied.
/// </summary>
public class ProxyTarget : BaseEntity
{
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, string> DefaultHeaders { get; set; } = [];
}
