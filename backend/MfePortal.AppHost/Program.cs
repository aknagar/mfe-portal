using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("infra");

var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

var productdb = postgres.AddDatabase("productdb", "productdb");
var weatherdb = postgres.AddDatabase("weatherdb", "weatherdb");

// Add AugmentService.Api with references
var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice")
    .WithReference(productdb)
    .WithReference(weatherdb)
    .WaitFor(productdb)
    .WaitFor(weatherdb);

// Only add Key Vault reference in non-development
if (!builder.Environment.IsDevelopment())
{
    // Add Key Vault - no provisioning, uses existing vault via configuration
    var keyVault = builder.AddAzureKeyVault("keyvault")
                    .PublishAsConnectionString();

    augmentService.WithReference(keyVault);
}

// Add Frontend container from Azure Container Registry
var frontendImage = builder.Environment.IsDevelopment() 
    ? "mfe-portal-frontend:latest"
    : "acrescmmynaae3lk.azurecr.io/frontend:latest";

var frontend = builder.AddContainer("frontend", frontendImage)
    .WithHttpEndpoint(port: builder.Environment.IsDevelopment() ? 1234 : 80, targetPort: 1234, name: "http")
    .WaitFor(augmentService);

builder.Build().Run();
