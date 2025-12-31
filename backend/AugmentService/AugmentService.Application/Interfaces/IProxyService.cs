namespace AugmentService.Application.Interfaces;

/// <summary>
/// Service for handling HTTP proxy requests.
/// </summary>
public interface IProxyService
{
    Task<HttpResponseMessage> ProxyRequestAsync(string targetUrl, HttpMethod method, HttpContent? content, CancellationToken cancellationToken = default);
    Task<string> ValidateUrlAsync(string url, CancellationToken cancellationToken = default);
}
