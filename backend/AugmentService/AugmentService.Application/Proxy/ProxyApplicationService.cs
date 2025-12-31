using AugmentService.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Proxy;

public class ProxyApplicationService : IProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProxyApplicationService> _logger;

    public ProxyApplicationService(HttpClient httpClient, ILogger<ProxyApplicationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HttpResponseMessage> ProxyRequestAsync(
        string targetUrl,
        HttpMethod method,
        HttpContent? content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(targetUrl);

        try
        {
            _logger.LogInformation("Proxying {Method} request to {TargetUrl}", method.Method, targetUrl);

            var request = new HttpRequestMessage(method, targetUrl)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);

            _logger.LogInformation(
                "Proxy request to {TargetUrl} completed with status {StatusCode}",
                targetUrl,
                response.StatusCode);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {TargetUrl}", targetUrl);
            throw;
        }
    }
}
