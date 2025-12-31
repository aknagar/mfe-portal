using AugmentService.Application.Interfaces;
using AugmentService.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register infrastructure services (dependency injection for clean architecture)
builder.Services.AddInfrastructureServices();

// Minimal local service defaults
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), new[] { "live" });

// OpenAPI support with Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Augment Service API",
        Version = "v1",
        Description = "Service for augmenting and proxying external requests with Clean Architecture"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Serve static files (for docs UI)
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Map proxy endpoint using the IProxyService from application layer
app.MapGet("/proxy", async (string url, IProxyService proxyService, ILogger<Program> logger) =>
{
    logger.LogInformation("Proxying GET request to {Url}", url);
    try
    {
        var response = await proxyService.ProxyRequestAsync(url, HttpMethod.Get, null);
        logger.LogInformation("Proxy response received with status {StatusCode}", response.StatusCode);
        
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/plain";
        if (contentType.StartsWith("text/") || contentType == "application/json")
        {
            var text = await response.Content.ReadAsStringAsync();
            return Results.Text(text, contentType);
        }

        var contentStream = await response.Content.ReadAsStreamAsync();
        return Results.Stream(contentStream, contentType: contentType);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error proxying request to {Url}", url);
        return Results.Problem($"Error proxying request: {ex.Message}");
    }
})
.WithName("ProxyRequest")
.WithSummary("Proxy HTTP GET request to external URL")
.WithDescription("Forwards an HTTP GET request to an external URL and returns the response content with the original content type.")
.WithOpenApi()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

// Health check endpoint
app.MapGet("/health-details", async (IProxyService proxyService) =>
{
    var urlValidation = await proxyService.ValidateUrlAsync("https://httpbin.org/get");
    return Results.Json(new
    {
        status = "healthy",
        service = "AugmentService",
        architecture = "Clean Architecture",
        layers = new[] { "Core", "Application", "Infrastructure", "API" },
        uptime = DateTime.UtcNow
    });
})
.WithName("HealthDetails")
.WithSummary("Get detailed health information")
.WithOpenApi();

app.Run();
