using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

var postgresdb = postgres.AddDatabase("postgresdb", "productdb");

// Add Azure Key Vault integration with environment-based configuration
// Development: Uses emulator (localhost:8200 or via Azure CLI)
// Production: Uses actual Azure Key Vault from ASPIRE_KEYVAULT_URI environment variable
var keyVault = builder.AddAzureKeyVault("keyvault");

// Add AugmentService.Api with references to PostgreSQL and Key Vault
builder.AddProject<Projects.AugmentService_Api>("augmentservice")
    .WithReference(postgresdb)
    .WithReference(keyVault)
    .WaitFor(postgresdb);

builder.Build().Run();
