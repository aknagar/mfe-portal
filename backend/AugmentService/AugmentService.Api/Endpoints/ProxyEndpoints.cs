using AugmentService.Core.Interfaces;

namespace AugmentService.Api.Endpoints;

public static class ProxyEndpoints
{
    public static IEndpointRouteBuilder MapProxyEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/proxy", ProxyHandler)
            .WithName("Proxy")
            .WithOpenApi()
            .Produces(200, typeof(Stream))
            .WithDescription("Forward HTTP GET requests to external URLs");

        return routes;
    }

    private static async Task<IResult> ProxyHandler(string url, IProxyService proxyService)
    {
        var response = await proxyService.ProxyRequestAsync(url, HttpMethod.Get, null);
        return Results.Stream(await response.Content.ReadAsStreamAsync(), 
            response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
    }
}
