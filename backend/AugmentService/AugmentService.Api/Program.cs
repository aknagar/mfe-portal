using AugmentService.Api.Routes.Weather;
using AugmentService.Api.Routes.Orders;
using Scalar.AspNetCore;
using Application;
using AugmentService.Api.Endpoints;
using Dapr.Workflow;
using Dapr.Actors;
using AugmentService.Api.Workflows;
using AugmentService.Api.Activities;
using AugmentService.Infrastructure;
using AugmentService.Infrastructure.ProductData;
using Azure.Identity;
using AugmentService.Infrastructure.WeatherData;

var builder = WebApplication.CreateBuilder(args);

// Create shared Azure credential for Key Vault and Database authentication
var credential = new DefaultAzureCredential();

builder.AddServiceDefaults();

builder.Services.AddOpenApi();  // OpenAPI is the next version swagger

builder.Services.AddControllers();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add other layers
builder.AddApplication();
builder.AddInfrastructure();

// Add Aspire Key Vault client integration
// Connects to the Key Vault resource defined in AppHost ("keyvault")
// Configuration comes from appsettings with key "Keyvault:Uri"
builder.AddAzureKeyVaultClient("keyvault", settings => 
{
    settings.DisableHealthChecks = true; // Optional: disable health checks if not needed
});

// Add Service Bus client
builder.AddAzureServiceBusClient("messaging");

// Log all environment variables injected by Aspire (Development only)
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("=== ALL ENVIRONMENT VARIABLES ===");
    foreach (var envVar in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>().OrderBy(e => e.Key))
    {
        var key = envVar.Key.ToString();
        var value = envVar.Value?.ToString();
        
        // Mask sensitive values
        if (key != null && (key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) || 
            key.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("KEY", StringComparison.OrdinalIgnoreCase)))
        {
            value = "***MASKED***";
        }
        
        Console.WriteLine($"{key} = {value}");
    }
    Console.WriteLine("=================================");
}

// The connection name "productdb" matches what we defined in AppHost
builder.AddNpgsqlDbContext<ProductDataContext>(connectionName: "productdb");
builder.AddNpgsqlDbContext<WeatherDatabaseContext>(connectionName: "weatherdb");

builder.Services.AddDaprClient();

// Add Dapr Workflow (requires actor runtime)
builder.Services.AddDaprWorkflow(options =>
{  
    options.RegisterWorkflow<OrderProcessingWorkflow>();
    
    // These are the activities that get invoked by the workflow(s).
    options.RegisterActivity<NotifyActivity>();
    options.RegisterActivity<ReserveInventoryActivity>();
    options.RegisterActivity<ProcessPaymentActivity>();
    options.RegisterActivity<UpdateInventoryActivity>();
});

var app = builder.Build();

#region HTTP Pipeline Configuration

// Verify Dapr Placement Service is running (required for workflows/actors)
//await VerifyDaprPlacementServiceAsync(app.Logger);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); //publish endpoint at /openapi/v1.json
    app.MapScalarApiReference(); // similar to swagger UI at /scalar/v1
};

app.UseCors(); // Enable CORS

app.MapDefaultEndpoints();

app.CreateProductDbIfNotExists();

// TODO: Configure WeatherDatabaseContext to use Aspire-injected connection string
 app.CreateWeatherDbIfNotExists();

/*
var secretClient = app.Services.GetService<SecretClient>();
// This is a plug and play mechanism where we are plugging /product endpoints
if (secretClient != null)
{
    app.MapProductEndpoints(secretClient);
}
*/

app.MapProductEndpoints();

// https://github.com/varianter/dotnet-template
app.MapWeatherUserGroup()
   .MapWeatherAdminGroup();

app.MapNotify();

app.MapControllers();

app.MapProxyEndpoints();

app.UseStaticFiles();

app.Run();

#endregion

static async Task VerifyDaprPlacementServiceAsync(ILogger logger)
{
    const string daprMetadataUrl = "http://localhost:3500/v1.0/metadata";
    const int maxRetries = 5;
    const int retryDelayMs = 1000;

    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Checking Dapr placement service connection (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
            
            var response = await httpClient.GetStringAsync(daprMetadataUrl);
            var metadata = System.Text.Json.JsonDocument.Parse(response);
            
            // Check actor runtime status
            if (metadata.RootElement.TryGetProperty("actorRuntime", out var actorRuntime))
            {
                var placement = actorRuntime.GetProperty("placement").GetString();
                var hostReady = actorRuntime.GetProperty("hostReady").GetBoolean();
                
                if (placement?.Contains("connected") == true && hostReady)
                {
                    logger.LogInformation("✅ Dapr placement service is connected and ready");
                    return;
                }
                
                logger.LogWarning("Dapr placement: {Placement}, hostReady: {HostReady}", placement, hostReady);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Dapr metadata endpoint not accessible: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Error checking Dapr placement: {Message}", ex.Message);
        }

        if (attempt < maxRetries)
        {
            await Task.Delay(retryDelayMs);
        }
    }

    logger.LogError("❌ CRITICAL: Dapr placement service is not connected after {MaxRetries} attempts", maxRetries);
    logger.LogError("Workflows and actors will NOT work without placement service.");
    logger.LogError("Solutions:");
    logger.LogError("  1. Restart AppHost with AppPort configured in DaprSidecarOptions");
    logger.LogError("  2. Ensure Dapr init has been run: dapr init");
    logger.LogError("  3. Check Docker containers: docker ps | Select-String placement");
    
    throw new InvalidOperationException(
        "Dapr placement service is not connected. Actors and workflows require a running placement service. " +
        "Please run 'dapr init' and restart the application.");
}
