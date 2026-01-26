using AugmentService.Api.Routes.Weather;
using AugmentService.Api.Routes.Orders;
using Scalar.AspNetCore;
using Application;
using AugmentService.Api.Endpoints;
using Dapr.Workflow;
using AugmentService.Api.Workflows;
using AugmentService.Api.Activities;
using AugmentService.Infrastructure;
using AugmentService.Infrastructure.ProductData;
using Azure.Identity;
using AugmentService.Infrastructure.WeatherData;
using Microsoft.OpenApi.Any;
using AugmentService.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Create shared Azure credential for Key Vault and Database authentication
var credential = new DefaultAzureCredential();

builder.AddServiceDefaults();

builder.Services.AddOpenApi(options =>
{
    // Add default example for Order schema in Scalar UI
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(Order))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("Paperclips"),
                ["totalCost"] = new OpenApiInteger(100),
                ["quantity"] = new OpenApiInteger(10)
            };
        }
        return Task.CompletedTask;
    });
});

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

// Add JWT Bearer Authentication for development
// In development, use a simple JWT scheme for testing
// In production, this should be replaced with Azure AD / Entra ID (Microsoft.Identity.Web)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Development: Allow requests without valid tokens for testing
            // This is a simplified setup for development - replace with Azure AD in production
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, parameters) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)
            };
            
            // For development: accept any token or no token
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // If no token is provided, create a test user for development
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        // Create claims for a test user in development mode
                        var claims = new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "[email protected]"),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Dev User" )
                        };
                        var identity = new System.Security.Claims.ClaimsIdentity(claims, "DevAuth");
                        context.Principal = new System.Security.Claims.ClaimsPrincipal(identity);
                        context.Success();
                    }
                    return Task.CompletedTask;
                }
            };
        }
        else
        {
            // Production TODO: Configure Azure AD / Entra ID
            // builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");
            throw new InvalidOperationException(
                "Azure AD authentication must be configured for production. " +
                "Add Microsoft.Identity.Web package and configure AzureAd section in appsettings.json");
        }
    });

builder.Services.AddAuthorization();

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
builder.AddNpgsqlDbContext<AugmentService.Infrastructure.Data.UserDbContext>(connectionName: "weatherdb");

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
    
    // Approval workflow activities
    options.RegisterActivity<RequestApprovalActivity>();
    options.RegisterActivity<HandleApprovalTimeoutActivity>();
});

// Add global exception handler
builder.Services.AddExceptionHandler<AugmentService.Api.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

#region HTTP Pipeline Configuration

// Verify Dapr Placement Service is running (required for workflows/actors)
//await VerifyDaprPlacementServiceAsync(app.Logger);

// Configure the HTTP request pipeline.
// Add exception handler middleware
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); //publish endpoint at /openapi/v1.json
    app.MapScalarApiReference(); // similar to swagger UI at /scalar/v1
};

app.UseHttpsRedirection(); // Enforce HTTPS-only

app.UseCors(); // Enable CORS

app.UseAuthentication(); // Enable authentication
app.UseAuthorization(); // Enable authorization

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
