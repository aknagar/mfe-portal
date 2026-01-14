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
    //.WithHttpEndpoint(port: 8080, name: "http")  // Explicit HTTP endpoint for Dapr AppPort
    .WithReference(productdb)
    .WithReference(weatherdb)
    .WithReference(serviceBus)
    .WithExternalHttpEndpoints()
    .WaitFor(productdb)
    .WaitFor(weatherdb)
    .WithDaprSidecar(new DaprSidecarOptions
    {
        ResourcesPaths = ["../dapr/components"],
        //AppPort = 8080,  // Port used by dapr to call the application - REQUIRED for actors/workflows
        DaprHttpPort = 3500, // Port used by the application to call dapr
        DaprGrpcPort = 50001
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
