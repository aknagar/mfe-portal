using Aspire.Hosting.Azure;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("infra");

var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

var productdb = postgres.AddDatabase("productdb", "productdb");
var weatherdb = postgres.AddDatabase("weatherdb", "weatherdb");

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
    .WithExternalHttpEndpoints()
    .WaitFor(productdb)
    .WaitFor(weatherdb)
    .WaitFor(serviceBus)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        ResourcesPaths = ["../dapr/components"],
        AppPort = 8080,  // REQUIRED for actors/workflows - Dapr needs to call the app
        DaprHttpPort = 3500, // Port for app to call Dapr HTTP API
        DaprGrpcPort = 50001 // Port for app to call Dapr gRPC API (workflows)
    });

// Only add Key Vault reference in non-development
if (!builder.Environment.IsDevelopment())
{
    // Add Key Vault - no provisioning, uses existing vault via configuration
    var keyVault = builder.AddAzureKeyVault("keyvault")
                    .PublishAsConnectionString();

    augmentService.WithReference(keyVault);
}

// Add Frontend container - use local image in development, ACR in production
var frontendImage = builder.Environment.IsDevelopment() 
    ? "frontend:latest" 
    : "infraacrescmmynaae3lk.azurecr.io/frontend:latest";

var frontend = builder.AddContainer("frontend", frontendImage)
    .WithHttpEndpoint(port: builder.Environment.IsDevelopment() ? 1234 : 80, targetPort: 1234, name: "http")
    .WithExternalHttpEndpoints()
    .WaitFor(augmentService);

builder.Build().Run();
