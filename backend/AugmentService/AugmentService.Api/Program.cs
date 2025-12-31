using AugmentService.Api.Routes.Weather;
using AugmentService.Api.Routes;
using Microsoft.Extensions.Azure;
using AugmentService.Api.Routes.Orders;
using Scalar.AspNetCore;
using Application;
using Microsoft.EntityFrameworkCore;
using AugmentService.Api.Endpoints;
using Azure.Security.KeyVault.Secrets;
using Dapr.Workflow;
using AugmentService.Api.Workflows;
using AugmentService.Api.Activities;
using AugmentService.Infrastructure;
using AugmentService.Infrastructure.ProductData;
using AugmentService.Infrastructure.WeatherData;
using AugmentService.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();  // OpenAPI is the next version swagger

builder.Services.AddControllers();

// Add Dapr Workflow
builder.Services.AddDaprWorkflow(options =>
{  
    options.RegisterWorkflow<OrderProcessingWorkflow>();
        
    // These are the activities that get invoked by the workflow(s).
    options.RegisterActivity<NotifyActivity>();
    options.RegisterActivity<ReserveInventoryActivity>();
    options.RegisterActivity<ProcessPaymentActivity>();
    options.RegisterActivity<UpdateInventoryActivity>();
});

// Add other layers
builder.AddApplication();
builder.AddInfrastructure();

//Add Keyvault client
//builder.AddAzureKeyVaultClient("secrets", settings => settings.DisableHealthChecks = true);

// Add Service Bus client
builder.AddAzureServiceBusClient("serviceBus");



builder.Services.AddDbContext<ProductDataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ProductsContext") ?? throw new InvalidOperationException("Connection string 'ProductsContext' not found.")));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); //publish endpoint at /openapi/v1.json
    app.MapScalarApiReference(); // similar to swagger UI at /scalar/v1
};

app.MapDefaultEndpoints();

app.CreateProductDbIfNotExists();

app.CreateWeatherDbIfNotExists();

var secretClient = app.Services.GetService<SecretClient>();

// This is a plug and play mechanism where we are plugging /product endpoints
app.MapProductEndpoints(secretClient);

// https://github.com/varianter/dotnet-template
app.MapWeatherUserGroup()
   .MapWeatherAdminGroup();

app.MapNotify();

app.MapControllers();

app.MapProxyEndpoints();

app.UseStaticFiles();

app.Run();

