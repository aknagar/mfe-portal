using MfePortal.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MfePortal.Application.Services;

/// <summary>
/// Application service for proxy operations - orchestrates business logic.
/// </summary>
public class ProxyApplicationService : IProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProxyApplicationService> _logger;

    public ProxyApplicationService(HttpClient httpClient, ILogger<ProxyApplicationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> ProxyRequestAsync(
        string targetUrl, 
        HttpMethod method, 
        HttpContent? content, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Proxying {Method} request to {TargetUrl}", method.Method, targetUrl);
            
            var request = new HttpRequestMessage(method, targetUrl)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            _logger.LogInformation("Proxy request completed with status {StatusCode}", response.StatusCode);
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to proxy request to {TargetUrl}", targetUrl);
            throw;
        }
    }

    public async Task<string> ValidateUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return "Invalid URL format";
            }

            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            return response.IsSuccessStatusCode ? "Valid" : $"HTTP {response.StatusCode}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "URL validation failed for {Url}", url);
            return $"Error: {ex.Message}";
        }
    }
}
