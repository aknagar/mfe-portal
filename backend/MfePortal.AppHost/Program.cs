using Microsoft.Extensions.Hosting;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();

// Configure PostgreSQL based on environment
if (isDevelopment)
{
    // Development: Use local PostgreSQL container managed by Aspire
    var postgres = builder.AddPostgres("postgres")
        .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

    var postgresdb = postgres.AddDatabase("postgresdb", "mfeportal");

    // Add AugmentService.Api with reference to local PostgreSQL database
    // Azure Key Vault will be accessed via DefaultAzureCredential in the service
    builder.AddProject<Projects.AugmentService_Api>("augmentservice")
        .WithReference(postgresdb)
        .WaitFor(postgresdb);
}
else
{
    // Test/Production: Use Azure PostgreSQL instance
    var azurePostgresConnectionString = builder.Configuration["ConnectionStrings:AzurePostgres"]
        ?? throw new InvalidOperationException(
            "Azure PostgreSQL connection string not configured. Set 'ConnectionStrings:AzurePostgres' in environment variables or Key Vault.");

    // Add reference to existing Azure PostgreSQL (no container creation)
    builder.AddConnectionString("postgresdb", azurePostgresConnectionString);

    // Add AugmentService.Api with reference to Azure PostgreSQL
    builder.AddProject<Projects.AugmentService_Api>("augmentservice");
}

builder.Build().Run();
