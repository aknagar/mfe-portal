namespace AugmentService.Core.Interfaces;

public interface IProxyService
{
    Task<HttpResponseMessage> ProxyRequestAsync(
        string targetUrl,
        HttpMethod method,
        HttpContent? content,
        CancellationToken cancellationToken = default);
}
