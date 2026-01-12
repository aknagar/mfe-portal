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

// Add Dapr Workflow - only in non-Development environments
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddDaprWorkflow(options =>
    {  
        options.RegisterWorkflow<OrderProcessingWorkflow>();
        
        // These are the activities that get invoked by the workflow(s).
        options.RegisterActivity<NotifyActivity>();
        options.RegisterActivity<ReserveInventoryActivity>();
        options.RegisterActivity<ProcessPaymentActivity>();
        options.RegisterActivity<UpdateInventoryActivity>();
    });
}

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
builder.AddAzureServiceBusClient("serviceBus");

// The connection name "productdb" matches what we defined in AppHost
builder.AddNpgsqlDbContext<ProductDataContext>(connectionName: "productdb");
builder.AddNpgsqlDbContext<WeatherDatabaseContext>(connectionName: "weatherdb");

var app = builder.Build();

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

