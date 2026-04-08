using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        // Configure OpenTelemetry logging — enables structured log export to the Aspire dashboard
        // (via OTLP) in local development, and to Azure Monitor when the connection string is present.
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        // OpenTelemetry metrics and tracing — required for the Aspire dashboard to show live
        // traces/metrics in local development via the OTLP exporter below.
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        // OTLP exporter — sends telemetry to the Aspire dashboard in local development.
        // OTEL_EXPORTER_OTLP_ENDPOINT is injected automatically by the Aspire AppHost.
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Azure Monitor (Application Insights) — only enabled when the connection string is present.
        // In local development this env var is absent, so this block is skipped entirely,
        // preventing the InvalidOperationException that UseAzureMonitor() throws without a
        // connection string.
        // APPLICATIONINSIGHTS_CONNECTION_STRING is injected by the Aspire AppHost in non-Development
        // environments via builder.AddAzureApplicationInsights(...) in AppHost/Program.cs.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            var clientId = builder.Configuration["AZURE_CLIENT_ID"];
            builder.Services.AddOpenTelemetry()
                .UseAzureMonitor(options =>
                {
                    // AZURE_CLIENT_ID must be set when using a User-Assigned Managed Identity.
                    // If absent, ManagedIdentityCredential falls back to system-assigned identity,
                    // which will fail if the container only has user-assigned identities.
                    options.Credential = new ManagedIdentityCredential(clientId);
                });
        }

        // Health checks
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });

        // Service discovery
        builder.Services.AddServiceDiscovery();

        // HttpClient defaults: resilience + service discovery
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        // Disable rate limiting for health checks
        app.MapHealthChecks("/health").DisableRateLimiting();

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        // Disable rate limiting for liveness checks
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        }).DisableRateLimiting();

        return app;
    }

}
