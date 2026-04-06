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
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using AugmentService.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

var isTest = builder.Environment.EnvironmentName == "Test";

// Create shared Azure credential for Key Vault and Database authentication
// (not needed — and causes IMDS probe hangs — in the Test environment)
DefaultAzureCredential? credential = isTest ? null : new DefaultAzureCredential();

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

// Authentication setup:
//   Development  — lenient JWT scheme; accepts any token or no token (test user auto-created)
//   All others   — Microsoft.Identity.Web validates tokens against Azure Entra ID (Azure AD)
//                  Reads TenantId, ClientId, and Audience from the "AzureAd" config section.
//                  Integration tests override this via WebApplicationFactory, replacing the
//                  JwtBearer handler with a lightweight test handler (see JwtTestAuthHandler).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Development: Allow requests without valid tokens for testing
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
                        var claims = new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "[email protected]"),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Dev User")
                        };
                        var identity = new System.Security.Claims.ClaimsIdentity(claims, "DevAuth");
                        context.Principal = new System.Security.Claims.ClaimsPrincipal(identity);
                        context.Success();
                    }
                    return Task.CompletedTask;
                }
            };
        });
}
else
{
    // Production: validate Bearer tokens against Azure Entra ID (Azure AD).
    // Reads Instance, TenantId, ClientId, and Audience from the "AzureAd" appsettings section.
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");
}

builder.Services.AddAuthorization();

// Add other layers
builder.AddApplication();
builder.AddInfrastructure();

// Add Aspire Key Vault client integration
// Connects to the Key Vault resource defined in AppHost ("keyvault")
// Configuration comes from appsettings with key "Keyvault:Uri"
// Skipped in Test environment to avoid Azure IMDS probe hangs.
if (!isTest)
{
    builder.AddAzureKeyVaultClient("keyvault", settings => 
    {
        settings.DisableHealthChecks = true; // Optional: disable health checks if not needed
    });

    // Add Service Bus client
    builder.AddAzureServiceBusClient("messaging");
}

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
// Skip Aspire database registrations in test environment (integration tests will register their own)
Console.WriteLine($"[Database Registration] Environment: {builder.Environment.EnvironmentName}");
if (builder.Environment.EnvironmentName != "Test")
{
    Console.WriteLine("[Database Registration] Registering Aspire PostgreSQL DbContexts");
    builder.AddNpgsqlDbContext<ProductDataContext>(connectionName: "productdb");
    builder.AddNpgsqlDbContext<WeatherDatabaseContext>(connectionName: "weatherdb");
    builder.AddNpgsqlDbContext<AugmentService.Infrastructure.Data.UserDbContext>(connectionName: "weatherdb");
}
else
{
    Console.WriteLine("[Database Registration] Skipping Aspire registration (Test environment)");
}

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

// Register IOrderWorkflowClient — thin wrapper so OrdersController doesn't depend
// on the concrete DaprWorkflowClient (which has no virtual methods and can't be mocked).
builder.Services.AddScoped<IOrderWorkflowClient>(sp =>
    new DaprOrderWorkflowClient(sp.GetRequiredService<DaprWorkflowClient>()));

// Register IDaprWorkflowClient — allows ApprovalsController to depend on an interface
// instead of the sealed DaprWorkflowClient, enabling unit testing with Moq.
builder.Services.AddScoped<IDaprWorkflowClient>(sp =>
    sp.GetRequiredService<DaprWorkflowClient>());

// Add global exception handler
builder.Services.AddExceptionHandler<AugmentService.Api.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure Rate Limiting
// Bind options into DI so that WebApplicationFactory overrides via ConfigureAppConfiguration take effect.
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.SectionName));

var rateLimitingOptions = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

if (rateLimitingOptions.Enabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            // Read options lazily from DI (IOptionsMonitor is singleton-safe) so that
            // WebApplicationFactory overrides via ConfigureAppConfiguration take effect.
            var opts = context.RequestServices
                .GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<RateLimitingOptions>>()
                .CurrentValue;

            // Partition by authenticated user name, or fall back to IP address
            var partitionKey = context.User.Identity?.Name 
                ?? context.Connection.RemoteIpAddress?.ToString() 
                ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = opts.PermitLimit,
                    Window = TimeSpan.FromSeconds(opts.WindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = opts.QueueLimit
                });
        });

        // Customize rejection response
        options.OnRejected = async (context, cancellationToken) =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            
            logger.LogWarning(
                "Rate limit exceeded for {User} from {IP} on {Method} {Path}",
                context.HttpContext.User.Identity?.Name ?? "anonymous",
                context.HttpContext.Connection.RemoteIpAddress,
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            TimeSpan? retryAfter = null;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
            {
                retryAfter = retryAfterValue;
                context.HttpContext.Response.Headers.RetryAfter = 
                    ((int)retryAfterValue.TotalSeconds).ToString();
            }

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "TooManyRequests",
                message = "Rate limit exceeded. Please try again later.",
                statusCode = 429,
                retryAfterSeconds = retryAfter?.TotalSeconds
            }, cancellationToken);
        };
    });
}

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

// Enable rate limiting (must be after UseCors and before UseAuthentication)
if (rateLimitingOptions.Enabled)
{
    app.UseRateLimiter();
}

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
