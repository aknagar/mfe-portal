namespace AugmentService.Core.Entities;

public class ProxyTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
