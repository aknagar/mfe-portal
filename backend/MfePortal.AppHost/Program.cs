using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

var postgresdb = postgres.AddDatabase("postgresdb", "productdb");

// Add AugmentService.Api with references
var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

// Only add Key Vault reference in non-development environments
if (builder.Environment.IsProduction())
{
    // Add Key Vault - no provisioning, uses existing vault via configuration
    var keyVault = builder.AddAzureKeyVault("keyvault")
                    .PublishAsConnectionString();
                    
    augmentService.WithReference(keyVault);
}

builder.Build().Run();
