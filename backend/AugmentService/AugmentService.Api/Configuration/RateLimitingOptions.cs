namespace AugmentService.Api.Configuration;

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "RateLimiting";
    
    /// <summary>
    /// Whether rate limiting is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maximum number of requests allowed per window.
    /// Default: 100
    /// </summary>
    public int PermitLimit { get; set; } = 100;
    
    /// <summary>
    /// Time window duration in seconds.
    /// Default: 60 seconds (1 minute)
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
    
    /// <summary>
    /// Number of requests that can be queued when limit is reached.
    /// Default: 2
    /// </summary>
    public int QueueLimit { get; set; } = 2;
}
