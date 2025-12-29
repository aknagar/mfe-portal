using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Minimal local service defaults (don't rely on external extension methods)
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
        Description = "Service for augmenting and proxying external requests"
    });

    // Include XML comments for documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Map basic health endpoints (only enabled in Development below)

// Serve static files (for docs UI)
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    // Health endpoints used in development
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });

    // Server-side Scalar UI integration (requires Scalar.AspNetCore package)
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/proxy", async (string url, HttpClient httpClient, ILogger<Program> logger) =>
{
    logger.LogInformation("Proxying request to {Url}", url);
    try 
    {
        var response = await httpClient.GetAsync(url);
        logger.LogInformation("Received response from {Url} with status code {StatusCode}", url, response.StatusCode);
        
        // For documentation purposes we declare a scalar string result.
        // In practice we stream the content and return the original content type when available.
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
.WithSummary("Proxy HTTP request to external URL")
.WithDescription("Forwards an HTTP GET request to an external URL and returns the response content with the original content type. Supports both text-based and binary responses.")
.WithOpenApi()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

app.Run();
