using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Redis;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Detect if we're running in Azure provisioning mode
// Azure Container Apps requires HTTP endpoints to use port 80
bool isAzureProvisioning = args.Contains("--publisher") ||
                          Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID") != null;

builder.AddAzureContainerAppEnvironment("infra");

var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

var productdb = postgres.AddDatabase("productdb", "productdb");
var weatherdb = postgres.AddDatabase("weatherdb", "weatherdb");

// Add Redis for DAPR components
var redis = builder.AddRedis("redis");


// Add Azure Service Bus - use emulator in development
var serviceBus = builder.AddAzureServiceBus("messaging");

if (builder.Environment.IsDevelopment())
{
    serviceBus.RunAsEmulator();
}

// Add queues for Dapr pubsub
serviceBus.AddServiceBusQueue("orders");

// Add AugmentService.Api with references
var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice")
    .WithReference(productdb)
    .WithReference(weatherdb)
    .WithReference(serviceBus)
    .WithReference(redis)
    .WithExternalHttpEndpoints()
    .WaitFor(productdb)
    .WaitFor(weatherdb)
    .WaitFor(serviceBus)
    .WaitFor(redis)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "augmentservice",  // REQUIRED - unique app ID for Dapr
        ResourcesPaths = ["../dapr/components"]
    });

// Only add Application Insights and Key Vault in non-development environments
if (!builder.Environment.IsDevelopment())
{
    // Add Application Insights - Aspire will manage provisioning
    var appInsights = builder.AddAzureApplicationInsights("appinsights");
    augmentService.WithReference(appInsights);

    // Add Key Vault - no provisioning, uses existing vault via configuration
    var keyVault = builder.AddAzureKeyVault("keyvault")
                    .PublishAsConnectionString();

    augmentService.WithReference(keyVault);
}

// Azure Container Apps requires HTTP endpoints to use port 80
// In local development, use port 1234 for convenience
// In Azure provisioning, always use port 80 (required by Azure Container Apps)
var frontendPort = isAzureProvisioning ? 80 : (builder.Environment.IsDevelopment() ? 1234 : 80);

var frontend = builder.AddDockerfile("frontend", "../../frontend", "Dockerfile")
    .WithHttpEndpoint(port: frontendPort, targetPort: 1234, name: "http")
    .WithExternalHttpEndpoints()
    .WaitFor(augmentService);

// Add Diagrid Dashboard for Dapr monitoring
// Azure Container Apps requires HTTP endpoints to use port 80
// In local development, use port 8080 for convenience
// In Azure provisioning, always use port 80 (required by Azure Container Apps)
var diagridPort = isAzureProvisioning ? 80 : (builder.Environment.IsDevelopment() ? 8080 : 80);

var diagridDashboard = builder.AddContainer("diagrid-dashboard", "ghcr.io/diagridio/diagrid-dashboard:latest")
    .WithHttpEndpoint(port: diagridPort, targetPort: 8080, name: "http")
    .WithExternalHttpEndpoints();

builder.Build().Run();
